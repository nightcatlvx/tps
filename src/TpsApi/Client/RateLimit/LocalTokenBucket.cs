namespace TpsApi.Client.RateLimit;

/// <summary>
/// 本地令牌桶：控制单位时间内的请求速率
/// </summary>
public class LocalTokenBucket
{
    private readonly int _capacity;
    private readonly double _refillRate;
    private double _tokens;
    private DateTime _lastRefill;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LocalTokenBucket(int capacity, double refillRate)
    {
        _capacity = capacity;
        _refillRate = refillRate;
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
    }

    public async Task<bool> TryAcquireAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            Refill();
            if (_tokens < 1) return false;
            _tokens -= 1;
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;
        _tokens = Math.Min(_capacity, _tokens + elapsed * _refillRate);
        _lastRefill = now;
    }
}
