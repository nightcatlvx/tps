namespace TpsApi.Client.RateLimit;

public interface IRateLimiter
{
    Task<RateLimitResult> AcquireAsync(
        string key,
        int windowSeconds,
        int maxRequests,
        int burstPerSecond,
        CancellationToken ct = default);
}
