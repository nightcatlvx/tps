using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace Common.Cache;

/// <summary>
/// Redis 工厂 — 从 IConfiguration 读取 Redis:{name}，按名懒加载 IRedisService
/// </summary>
public static class RedisFactory
{
    private static readonly ConcurrentDictionary<string, IRedisService> _instances = new();
    private static IConfiguration? _config;

    public static void Initialize(IConfiguration config) => _config = config;

    public static IRedisService Get(string name = "Default")
    {
        return _instances.GetOrAdd(name, key =>
        {
            var connStr = _config?[$"Redis:{key}"]
                ?? throw new InvalidOperationException($"未配置 Redis:{key}");

            return new RedisService(connStr);
        });
    }
}
