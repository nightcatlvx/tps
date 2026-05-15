using Common.Cache;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace TpsApi.Client.RateLimit;

/// <summary>
/// Redis 令牌桶限流器（Lua 脚本原子操作）
/// </summary>
public class RedisRateLimiter : IRateLimiter
{
    private readonly ILogger<RedisRateLimiter> _logger;

    private const string LuaScript = """
        local key      = KEYS[1]
        local capacity = tonumber(ARGV[1])
        local rate     = tonumber(ARGV[2])
        local now_ms   = tonumber(ARGV[3])

        local tokens   = tonumber(redis.call('HGET', key, 'tokens')) or capacity
        local last_ms  = tonumber(redis.call('HGET', key, 'last_ms')) or now_ms

        local elapsed  = (now_ms - last_ms) / 1000.0
        tokens         = math.min(capacity, tokens + elapsed * rate)

        if tokens >= 1 then
            redis.call('HSET', key, 'tokens', tokens - 1, 'last_ms', now_ms)
            redis.call('EXPIRE', key, math.ceil(capacity / rate) + 10)
            return 1
        end

        redis.call('EXPIRE', key, math.ceil(capacity / rate) + 10)
        return 0
        """;

    public RedisRateLimiter(ILogger<RedisRateLimiter> logger)
    {
        _logger = logger;
    }

    public async Task<RateLimitResult> AcquireAsync(
        string key,
        int windowSeconds,
        int maxRequests,
        int burstPerSecond,
        CancellationToken ct = default)
    {
        var redis = (RedisService)RedisFactory.Get();
        var db = redis.Database;

        double refillRate = (double)maxRequests / windowSeconds;
        var redisKey = $"tps:ratelimit:{key}:{burstPerSecond}:{refillRate:F4}";

        var allowed = (int)await db.ScriptEvaluateAsync(
            LuaScript,
            [redisKey],
            [burstPerSecond, refillRate, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()]
        ) == 1;

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
