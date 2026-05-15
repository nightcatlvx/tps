using Microsoft.Extensions.Logging;

namespace TpsApi.Client.RateLimit;

public class LocalRateLimiter : IRateLimiter
{
    private readonly LocalTokenBucketStore _bucketStore;
    private readonly ILogger<LocalRateLimiter> _logger;

    public LocalRateLimiter(LocalTokenBucketStore bucketStore, ILogger<LocalRateLimiter> logger)
    {
        _bucketStore = bucketStore;
        _logger = logger;
    }

    public async Task<RateLimitResult> AcquireAsync(
        string key,
        int windowSeconds,
        int maxRequests,
        int burstPerSecond,
        CancellationToken ct = default)
    {
        double refillRate = (double)maxRequests / windowSeconds;

        var bucket = _bucketStore.GetOrCreate(key, burstPerSecond, refillRate);
        var allowed = await bucket.TryAcquireAsync(ct);

        if (!allowed)
        {
            _logger.LogWarning(
                "[RateLimit] 频次超限 key={Key} Window={Window}s Max={Max} Burst={Burst}",
                key, windowSeconds, maxRequests, burstPerSecond);
        }

        return new RateLimitResult
        {
            IsAllowed = allowed,
            Reason = allowed ? string.Empty : $"已触发限流策略({maxRequests}次/{windowSeconds}秒)"
        };
    }
}
