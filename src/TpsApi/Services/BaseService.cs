using Common.Cache;
using Common.DI;

namespace TpsApi.Services;

/// <summary>
/// 服务基类
/// </summary>
public abstract class BaseService : IBaseAutofac
{
    protected IRedisService Redis => RedisFactory.Get("Default");
    protected static IRedisService GetRedis(string name) => RedisFactory.Get(name);
}
