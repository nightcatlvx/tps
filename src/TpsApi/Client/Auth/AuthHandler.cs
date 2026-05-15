using TpsApi.Client.Core;
using Microsoft.Extensions.Logging;

namespace TpsApi.Client.Auth;

/// <summary>
/// 授权 DelegatingHandler
/// 挂入 HttpClient 管道，调用 IServiceAuth.InjectAsync 注入 Token
/// 收到 401 时清除缓存并重试一次
/// </summary>
public class AuthHandler : DelegatingHandler
{
    private readonly IEnumerable<IServiceAuth> _authProviders;
    private readonly ILogger<AuthHandler> _logger;
    internal static readonly AsyncLocal<RequestMetadata?> CurrentMeta = new();

    public AuthHandler(IEnumerable<IServiceAuth> authProviders, ILogger<AuthHandler> logger)
    {
        _authProviders = authProviders;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var meta = CurrentMeta.Value;

        if (meta == null || !meta.IsAuthRequired)
            return await base.SendAsync(request, ct);

        var auth = _authProviders.FirstOrDefault(p => p.ServiceCode == meta.ServiceCode)
            ?? throw new InvalidOperationException(
                $"未找到 ServiceCode={meta.ServiceCode} 的授权实现");

        if (request.Content != null)
            await request.Content.LoadIntoBufferAsync(ct);

        await auth.InjectAsync(request, ct);
        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("[Auth] 收到 401，清除 Token 缓存后重试 ServiceCode={ServiceCode}", meta.ServiceCode);

            auth.Invalidate();

            var retryRequest = await CloneRequestAsync(request);
            await auth.InjectAsync(retryRequest, ct);
            response = await base.SendAsync(retryRequest, ct);
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (original.Content != null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
