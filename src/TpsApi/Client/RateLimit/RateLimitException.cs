namespace TpsApi.Client.RateLimit;

public class RateLimitException(string serviceCode, string funcCode, string reason)
    : Exception($"[RateLimit] 触发拦截 ServiceCode={serviceCode} FuncCode={funcCode} Reason={reason}")
{
    public string ServiceCode { get; } = serviceCode;
    public string FuncCode { get; } = funcCode;
    public string Reason { get; } = reason;
}
