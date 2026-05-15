using System.Reflection;
using TpsApi.Attributes;

namespace TpsApi.Client.Core;

/// <summary>
/// 请求元数据 — 从 Attribute 解析后贯穿整个请求链路
/// </summary>
public class RequestMetadata
{
    public string ServiceCode { get; init; } = string.Empty;
    public string FuncCode { get; init; } = string.Empty;
    public string MethodName { get; init; } = string.Empty;
    public string HttpMethod { get; init; } = "GET";
    public string ContentType { get; init; } = "application/json";
    public string Path { get; init; } = string.Empty;

    public RetryPolicyAttribute? RetryPolicy { get; init; }
    public CircuitBreakerAttribute? CircuitBreaker { get; init; }
    public TimeoutAttribute? Timeout { get; init; }
    public NeedAuthAttribute? NeedAuth { get; init; }
    public NeedSignAttribute? NeedSign { get; init; }
    public SdkMethodAttribute? SdkMethod { get; init; }

    public bool IsAuthRequired => NeedAuth?.Required == true;
    public bool IsSignRequired => NeedSign?.Required == true;
    public bool IsSdkMethod => SdkMethod != null;

    /// <summary>
    /// 从方法和接口上的 Attribute 解析元数据
    /// 优先级：方法级 > 接口级
    /// </summary>
    public static RequestMetadata Resolve<TClient>(MethodInfo method)
    {
        T? Get<T>() where T : Attribute
            => method.GetCustomAttribute<T>()
            ?? typeof(TClient).GetCustomAttribute<T>();

        var apiMethod = Get<ApiMethodAttribute>();

        return new RequestMetadata
        {
            ServiceCode = Get<ServiceCodeAttribute>()?.Code ?? string.Empty,
            FuncCode = Get<FuncCodeAttribute>()?.Code ?? string.Empty,
            MethodName = method.Name,
            HttpMethod = apiMethod?.Method ?? "GET",
            ContentType = apiMethod?.ContentType ?? "application/json",
            Path = Get<ApiPathAttribute>()?.Path ?? string.Empty,
            RetryPolicy = Get<RetryPolicyAttribute>(),
            CircuitBreaker = Get<CircuitBreakerAttribute>(),
            Timeout = Get<TimeoutAttribute>(),
            NeedAuth = Get<NeedAuthAttribute>(),
            NeedSign = Get<NeedSignAttribute>(),
            SdkMethod = method.GetCustomAttribute<SdkMethodAttribute>(),
        };
    }
}
