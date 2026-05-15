using Common.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Data;

public static class ServiceExtensions
{
    /// <summary>
    /// 初始化 DbFactory + RedisFactory（从 appsettings.json 读取配置）
    /// </summary>
    public static IServiceCollection AddTpsInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        DbFactory.Initialize(config);
        RedisFactory.Initialize(config);
        return services;
    }
}
