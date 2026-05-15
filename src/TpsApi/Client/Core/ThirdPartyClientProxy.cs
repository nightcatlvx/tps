using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Common.Models;
using TpsApi.Attributes;
using TpsApi.Client.Auth;
using TpsApi.Client.RateLimit;
using TpsApi.Client.Sdk;
using TpsApi.Client.Signing;
using TpsApi.Repositories;
using Polly;
using Microsoft.Extensions.Logging;

namespace TpsApi.Client.Core;

public class ThirdPartyClientProxy<TClient> : DispatchProxy
{
    private IHttpClientFactory _httpFactory = null!;
    private ServiceConfigRepository _serviceConfigRepo = null!;
    private FuncConfigRepository _funcConfigRepo = null!;
    private RateLimitRuleRepository _rateLimitRuleRepo = null!;
    private IRateLimiter _rateLimiter = null!;
    private IEnumerable<IServiceAuth> _authProviders = null!;
    private IEnumerable<IRequestSigner> _signers = null!;
    private IServiceProvider _serviceProvider = null!;
    private ILogger _logger = null!;

    private static readonly ConcurrentDictionary<string, IAsyncPolicy<HttpResponseMessage>?>
        _policyCache = new();

    private static readonly ConcurrentDictionary<string, RequestMetadata>
        _metaCache = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static TClient Create(
        IHttpClientFactory httpFactory,
        ServiceConfigRepository serviceConfigRepo,
        FuncConfigRepository funcConfigRepo,
        RateLimitRuleRepository rateLimitRuleRepo,
        IRateLimiter rateLimiter,
        IEnumerable<IServiceAuth> authProviders,
        IEnumerable<IRequestSigner> signers,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        var proxy = Create<TClient, ThirdPartyClientProxy<TClient>>()
                    as ThirdPartyClientProxy<TClient>;

        proxy!._httpFactory = httpFactory;
        proxy._serviceConfigRepo = serviceConfigRepo;
        proxy._funcConfigRepo = funcConfigRepo;
        proxy._rateLimitRuleRepo = rateLimitRuleRepo;
        proxy._rateLimiter = rateLimiter;
        proxy._authProviders = authProviders;
        proxy._signers = signers;
        proxy._serviceProvider = serviceProvider;
        proxy._logger = logger;

        return (TClient)(object)proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        var meta = _metaCache.GetOrAdd(
            method!.Name,
            _ => RequestMetadata.Resolve<TClient>(method));

        if (string.IsNullOrEmpty(meta.ServiceCode))
            throw new InvalidOperationException(
                $"方法 {method.Name} 缺少 [ServiceCode] 标注");

        var returnType = method.ReturnType.GenericTypeArguments[0];
        return typeof(ThirdPartyClientProxy<TClient>)
            .GetMethod(nameof(DispatchAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(returnType)
            .Invoke(this, [method, args, meta])!;
    }

    private async Task<T> DispatchAsync<T>(MethodInfo method, object?[]? args, RequestMetadata meta)
    {
        var funcConfig = await CheckRateLimitAsync(meta);
        return meta.IsSdkMethod
            ? await InvokeSdkCoreAsync<T>(method, args, meta)
            : await SendAsync<T>(method, args, meta, funcConfig);
    }

    // ── 限流 ────────────────────────────────────────────────────
    private async Task<TpsFuncConfigDO?> CheckRateLimitAsync(RequestMetadata meta)
    {
        if (string.IsNullOrEmpty(meta.FuncCode))
            return null;

        var funcConfig = await _funcConfigRepo.GetByFuncCodeAsync(meta.FuncCode);
        if (funcConfig == null) return null;

        var rule = await _rateLimitRuleRepo.GetByFuncIdAsync(funcConfig.id);
        if (rule == null) return funcConfig;

        var key = $"{meta.ServiceCode}:{meta.FuncCode}";
        var result = await _rateLimiter.AcquireAsync(
            key, rule.window_seconds, rule.max_requests, rule.burst_per_second);

        if (!result.IsAllowed)
        {
            _logger.LogWarning(
                "[RateLimit] 触发拦截 ServiceCode={ServiceCode} FuncCode={FuncCode} Reason={Reason}",
                meta.ServiceCode, meta.FuncCode, result.Reason);

            throw new RateLimitException(meta.ServiceCode, meta.FuncCode, result.Reason);
        }

        return funcConfig;
    }

    // ── HTTP ────────────────────────────────────────────────────
    private async Task<T> SendAsync<T>(MethodInfo method, object?[]? args, RequestMetadata meta,
        TpsFuncConfigDO? funcConfig = null)
    {
        var serviceConfig = await _serviceConfigRepo.GetByServiceCodeAsync(meta.ServiceCode)
            ?? throw new InvalidOperationException($"未找到服务配置 ServiceCode={meta.ServiceCode}");

        var path = meta.Path;
        if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(meta.FuncCode))
        {
            funcConfig ??= await _funcConfigRepo.GetByFuncCodeAsync(meta.FuncCode)
                ?? throw new InvalidOperationException($"未找到功能配置 FuncCode={meta.FuncCode}");
            path = funcConfig.path;
        }

        var (url, bodyObj) = BuildRequest(method, args, serviceConfig.base_url, path, meta);

        var policy = _policyCache.GetOrAdd(method.Name, _ => PolicyBuilder.Build(meta, _logger));

        AuthHandler.CurrentMeta.Value = meta;
        SignHandler.CurrentMeta.Value = meta;

        var client = _httpFactory.CreateClient(meta.ServiceCode);

        HttpRequestMessage BuildMessage()
        {
            var req = new HttpRequestMessage(new HttpMethod(meta.HttpMethod), url);
            if (bodyObj != null)
            {
                req.Content = meta.ContentType switch
                {
                    "application/x-www-form-urlencoded" => new FormUrlEncodedContent(ToFormData(bodyObj)),
                    _ => new StringContent(JsonSerializer.Serialize(bodyObj, _jsonOptions), Encoding.UTF8, "application/json")
                };
            }
            return req;
        }

        _logger.LogInformation("[ThirdParty] {Method} {Url} Service={ServiceCode}", meta.HttpMethod, url, meta.ServiceCode);

        HttpResponseMessage response;
        if (policy != null)
            response = await policy.ExecuteAsync(() => client.SendAsync(BuildMessage()));
        else
            response = await client.SendAsync(BuildMessage());

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("[ThirdParty] 响应失败 Status={Status} Body={Body}", response.StatusCode, json);
            response.EnsureSuccessStatusCode();
        }

        return JsonSerializer.Deserialize<T>(json, _jsonOptions)
               ?? throw new InvalidOperationException("响应反序列化为空");
    }

    // ── SDK ─────────────────────────────────────────────────────
    private async Task<T> InvokeSdkCoreAsync<T>(MethodInfo method, object?[]? args, RequestMetadata meta)
    {
        var serviceConfig = await _serviceConfigRepo.GetByServiceCodeAsync(meta.ServiceCode)
            ?? throw new InvalidOperationException($"未找到服务配置 ServiceCode={meta.ServiceCode}");

        var adapter = _serviceProvider.GetRequiredService(meta.SdkMethod!.AdapterType);

        if (adapter is ISdkAdapter sdkAdapter)
            sdkAdapter.Initialize(serviceConfig);

        var targetMethodName = meta.SdkMethod.MethodName ?? method.Name;
        var targetMethod = meta.SdkMethod.AdapterType.GetMethod(targetMethodName)
            ?? throw new InvalidOperationException($"SDK方法 {targetMethodName} 未找到");

        var convertedArgs = ConvertSdkArgs(method, targetMethod, args);
        return await (Task<T>)targetMethod.Invoke(adapter, convertedArgs)!;
    }

    // ── 请求构建 ────────────────────────────────────────────────
    private static (string url, object? body) BuildRequest(
        MethodInfo method, object?[]? args, string baseUrl, string path, RequestMetadata meta)
    {
        var parameters = method.GetParameters();
        var queryParams = new Dictionary<string, string>();
        object? bodyObj = null;

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var value = args?[i];
            if (value == null) continue;

            if (param.GetCustomAttribute<PathParamAttribute>() is { } pp)
            {
                var key = string.IsNullOrEmpty(pp.Name) ? param.Name! : pp.Name;
                path = path.Replace($"{{{key}}}", Uri.EscapeDataString(value.ToString()!));
                continue;
            }

            if (param.GetCustomAttribute<BodyAttribute>() != null)
            {
                bodyObj = value;
                continue;
            }

            if (meta.HttpMethod == "GET" || param.GetCustomAttribute<QueryAttribute>() != null)
                FlattenToQuery(value, param.Name!, queryParams);
        }

        var url = baseUrl.TrimEnd('/') + path;
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return (url, bodyObj);
    }

    private static void FlattenToQuery(object obj, string paramName, Dictionary<string, string> result)
    {
        if (obj is string or ValueType)
        {
            result[paramName] = obj.ToString()!;
            return;
        }

        foreach (var prop in obj.GetType().GetProperties())
        {
            var val = prop.GetValue(obj);
            if (val != null)
                result[prop.Name] = val.ToString()!;
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> ToFormData(object obj)
        => obj.GetType().GetProperties()
              .Where(p => p.GetValue(obj) != null)
              .Select(p => new KeyValuePair<string, string>(p.Name, p.GetValue(obj)!.ToString()!));

    private static object?[] ConvertSdkArgs(MethodInfo sourceMethod, MethodInfo targetMethod, object?[]? args)
    {
        var sourceParams = sourceMethod.GetParameters();
        var targetParams = targetMethod.GetParameters();
        if (sourceParams.Length == targetParams.Length)
            return args ?? [];

        var dto = args?.FirstOrDefault();
        if (dto == null) return [];

        var dtoProps = dto.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(dto), StringComparer.OrdinalIgnoreCase);

        return targetParams.Select(p => dtoProps.TryGetValue(p.Name!, out var val) ? val : null).ToArray();
    }
}
