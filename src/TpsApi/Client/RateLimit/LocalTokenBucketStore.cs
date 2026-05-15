using System.Collections.Concurrent;

namespace TpsApi.Client.RateLimit;

public class LocalTokenBucketStore
{
    private readonly ConcurrentDictionary<string, LocalTokenBucket> _buckets = new();

    public LocalTokenBucket GetOrCreate(string key, int capacity, double refillRate)
    {
        var bucketKey = $"{key}:{capacity}:{refillRate:F4}";
        return _buckets.GetOrAdd(bucketKey, _ => new LocalTokenBucket(capacity, refillRate));
    }
}
