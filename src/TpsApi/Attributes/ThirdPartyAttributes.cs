namespace TpsApi.Attributes;

/// <summary>
/// 平台标识，对应 service_config.service_code
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public class ServiceCodeAttribute(string code) : Attribute
{
    public string Code { get; } = code;
}

/// <summary>
/// 功能标识，对应 func_config.func_code
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FuncCodeAttribute(string code) : Attribute
{
    public string Code { get; } = code;
}

/// <summary>
/// HTTP 请求方法和内容类型
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ApiMethodAttribute : Attribute
{
    public string Method { get; set; } = "GET";
    public string ContentType { get; set; } = "application/json";
}

/// <summary>
/// HTTP 请求路径，支持 {占位符}
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ApiPathAttribute(string path) : Attribute
{
    public string Path { get; } = path;
}

/// <summary>
/// 重试策略
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public class RetryPolicyAttribute : Attribute
{
    public int RetryCount { get; set; } = 3;
    public double[] WaitSeconds { get; set; } = [];
    public bool ExponentialBackoff { get; set; } = true;
}

/// <summary>
/// 熔断策略
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public class CircuitBreakerAttribute : Attribute
{
    public int BreakAfterFaults { get; set; } = 5;
    public int BreakDurationSeconds { get; set; } = 30;
}

/// <summary>
/// 超时策略
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public class TimeoutAttribute : Attribute
{
    public int TimeoutMs { get; set; } = 5000;
}

/// <summary>
/// URL 路径占位符参数
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class PathParamAttribute(string name = "") : Attribute
{
    public string Name { get; } = name;
}

/// <summary>
/// 标记参数为请求 Body
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class BodyAttribute : Attribute { }

/// <summary>
/// 标记参数为 Query String
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class QueryAttribute : Attribute { }

/// <summary>
/// 标记方法走 SDK 调用而非 HTTP
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SdkMethodAttribute(Type adapterType) : Attribute
{
    public Type AdapterType { get; } = adapterType;
    public string? MethodName { get; set; }
}
