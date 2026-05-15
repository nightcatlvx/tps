namespace Common.Cache;

/// <summary>
/// Redis 服务接口
/// </summary>
public interface IRedisService
{
    // String
    Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null);
    Task<string?> StringGetAsync(string key);
    Task<long> StringIncrementAsync(string key);
    Task<bool> StringDeleteAsync(string key);

    // Hash
    Task<bool> HashSetAsync(string hashKey, string field, string value);
    Task<string?> HashGetAsync(string hashKey, string field);
    Task<Dictionary<string, string>> HashGetAllAsync(string hashKey);
    Task<bool> HashDeleteAsync(string hashKey, string field);

    // Set
    Task<bool> SetAddAsync(string key, string value);
    Task<List<string>> SetMembersAsync(string key);
    Task<bool> SetRemoveAsync(string key, string value);

    // List
    Task<long> ListPushAsync(string key, string value);
    Task<string?> ListPopAsync(string key);
    Task<List<string>> ListRangeAsync(string key, long start, long stop);

    // General
    Task<bool> KeyExistsAsync(string key);
    Task<bool> KeyExpireAsync(string key, TimeSpan expiry);
    Task<bool> KeyDeleteAsync(string key);
}
