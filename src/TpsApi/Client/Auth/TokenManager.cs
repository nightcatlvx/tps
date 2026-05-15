using System.Collections.Concurrent;
using System.Text.Json;
using Common.Cache;
using Microsoft.Extensions.Logging;

namespace TpsApi.Client.Auth;

/// <summary>
/// Token 管理器：Redis 缓存 + 本地并发锁防击穿
/// </summary>
public class TokenManager
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ILogger<TokenManager> _logger;

    private const string KeyPrefix = "tps:token";

    public TokenManager(ILogger<TokenManager> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetOrRefreshAsync(
        string serviceCode,
        Func<Task<TokenResult>> fetchFunc,
        CancellationToken ct = default)
    {
        var redis = RedisFactory.Get();
        var cacheKey = $"{KeyPrefix}:{serviceCode}";

        // 从 Redis 读取
        var cached = await TryGetFromRedis(redis, cacheKey);
        if (cached != null) return cached;

        // 本地锁防并发刷新
        var semaphore = _locks.GetOrAdd(serviceCode, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            cached = await TryGetFromRedis(redis, cacheKey);
            if (cached != null) return cached;

            _logger.LogInformation("[TokenManager] 刷新 Token ServiceCode={ServiceCode}", serviceCode);

            var result = await fetchFunc();
            await SetToRedis(redis, cacheKey, result);

            return result.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TokenManager] 获取 Token 失败 ServiceCode={ServiceCode}", serviceCode);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Invalidate(string serviceCode)
    {
        var redis = RedisFactory.Get();
        _ = redis.StringDeleteAsync($"{KeyPrefix}:{serviceCode}");
        _logger.LogInformation("[TokenManager] Token 缓存已清除 ServiceCode={ServiceCode}", serviceCode);
    }

    private static async Task<string?> TryGetFromRedis(IRedisService redis, string key)
    {
        var json = await redis.StringGetAsync(key);
        if (string.IsNullOrEmpty(json)) return null;

        var result = JsonSerializer.Deserialize<TokenResult>(json);
        if (result == null || result.IsExpired) return null;

        return result.Token;
    }

    private static async Task SetToRedis(IRedisService redis, string key, TokenResult result)
    {
        var json = JsonSerializer.Serialize(result);
        var ttl = result.ExpiredAt - DateTime.UtcNow;
        await redis.StringSetAsync(key, json, ttl > TimeSpan.Zero ? ttl : null);
    }
}
