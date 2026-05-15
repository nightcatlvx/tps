namespace TpsApi.Client.RateLimit;

public class RateLimitResult
{
    public bool IsAllowed { get; init; }
    public string Reason { get; init; } = string.Empty;
}
