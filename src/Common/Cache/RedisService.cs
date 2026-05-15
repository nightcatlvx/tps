using StackExchange.Redis;
using System.Text.Json;

namespace Common.Cache;

/// <summary>
/// Redis 基础封装 (String, Hash, Set, List)
/// </summary>
public class RedisService : IRedisService, IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    /// <summary>原始 IDatabase，供 Lua 脚本等高级操作</summary>
    public IDatabase Database => _db;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisService(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
    }

    // ── String ──────────────────────────────────────────────────

    public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null)
        => await _db.StringSetAsync(key, value, expiry);

    public async Task<string?> StringGetAsync(string key)
        => await _db.StringGetAsync(key);

    public async Task<long> StringIncrementAsync(string key)
        => await _db.StringIncrementAsync(key);

    public async Task<bool> StringDeleteAsync(string key)
        => await _db.KeyDeleteAsync(key);

    // ── Hash ────────────────────────────────────────────────────

    public async Task<bool> HashSetAsync(string hashKey, string field, string value)
        => await _db.HashSetAsync(hashKey, field, value);

    public async Task<string?> HashGetAsync(string hashKey, string field)
    {
        var val = await _db.HashGetAsync(hashKey, field);
        return val.ToString();
    }

    public async Task<Dictionary<string, string>> HashGetAllAsync(string hashKey)
    {
        var entries = await _db.HashGetAllAsync(hashKey);
        return entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
    }

    public async Task<bool> HashDeleteAsync(string hashKey, string field)
        => await _db.HashDeleteAsync(hashKey, field);

    // ── Set ─────────────────────────────────────────────────────

    public async Task<bool> SetAddAsync(string key, string value)
        => await _db.SetAddAsync(key, value);

    public async Task<List<string>> SetMembersAsync(string key)
    {
        var members = await _db.SetMembersAsync(key);
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task<bool> SetRemoveAsync(string key, string value)
        => await _db.SetRemoveAsync(key, value);

    // ── List ────────────────────────────────────────────────────

    public async Task<long> ListPushAsync(string key, string value)
        => await _db.ListLeftPushAsync(key, value);

    public async Task<string?> ListPopAsync(string key)
    {
        var val = await _db.ListRightPopAsync(key);
        return val.ToString();
    }

    public async Task<List<string>> ListRangeAsync(string key, long start, long stop)
    {
        var vals = await _db.ListRangeAsync(key, start, stop);
        return vals.Select(v => v.ToString()).ToList();
    }

    // ── General ─────────────────────────────────────────────────

    public async Task<bool> KeyExistsAsync(string key)
        => await _db.KeyExistsAsync(key);

    public async Task<bool> KeyExpireAsync(string key, TimeSpan expiry)
        => await _db.KeyExpireAsync(key, expiry);

    public async Task<bool> KeyDeleteAsync(string key)
        => await _db.KeyDeleteAsync(key);

    public void Dispose()
    {
        _redis?.Dispose();
    }
}
