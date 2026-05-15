using System.Reflection;
using TpsApi.Attributes;
using TpsApi.Client;
using TpsApi.Client.Auth;
using TpsApi.Client.Core;
using TpsApi.Client.RateLimit;
using TpsApi.Client.Signing;
using TpsApi.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace TpsApi.Extensions;

/// <summary>
/// 第三方服务客户端注册扩展
/// </summary>
public static class ThirdPartyClientExtension
{
    /// <summary>
    /// 注册所有第三方服务相关组件
    /// </summary>
    public static IServiceCollection AddThirdPartyClients(this IServiceCollection services)
    {
        // ── DAL 自动扫描 ────────────────────────────────────────
        foreach (var dalType in Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, Namespace: not null }
                        && t.Namespace.StartsWith("TpsApi.Repositories")))
        {
            services.AddScoped(dalType);
        }

        // ── 限流（Redis 令牌桶）──────────────────────────────────
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();

        // ── Token 管理 ──────────────────────────────────────────
        services.AddSingleton<TokenManager>();

        // ── Handler ─────────────────────────────────────────────
        services.AddScoped<AuthHandler>();
        services.AddScoped<SignHandler>();

        // ── HttpClient ──────────────────────────────────────────
        RegisterHttpClients(services);

        // ── 代理接口 ────────────────────────────────────────────
        RegisterClientProxies(services, Assembly.GetExecutingAssembly());

        return services;
    }

    private static void RegisterHttpClients(IServiceCollection services)
    {
        var serviceCodes = typeof(IExternalClient)
            .GetMethods()
            .Select(m => m.GetCustomAttribute<ServiceCodeAttribute>()?.Code)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct();

        foreach (var code in serviceCodes)
        {
            services
                .AddHttpClient(code!)
                .AddHttpMessageHandler<SignHandler>()
                .AddHttpMessageHandler<AuthHandler>();
        }
    }

    private static void RegisterClientProxies(IServiceCollection services, Assembly assembly)
    {
        var clientInterfaces = assembly.GetTypes()
            .Where(t => t.IsInterface && HasServiceCodeMethod(t));

        foreach (var interfaceType in clientInterfaces)
            RegisterProxy(services, interfaceType);
    }

    private static void RegisterProxy(IServiceCollection services, Type interfaceType)
    {
        services.AddScoped(interfaceType, sp =>
        {
            var createMethod = typeof(ThirdPartyClientProxy<>)
                .MakeGenericType(interfaceType)
                .GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!;

            return createMethod.Invoke(null, [
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ServiceConfigRepository>(),
                sp.GetRequiredService<FuncConfigRepository>(),
                sp.GetRequiredService<RateLimitRuleRepository>(),
                sp.GetRequiredService<IRateLimiter>(),
                sp.GetRequiredService<IEnumerable<IServiceAuth>>(),
                sp.GetRequiredService<IEnumerable<IRequestSigner>>(),
                sp,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger(interfaceType.Name)
            ])!;
        });
    }

    private static bool HasServiceCodeMethod(Type interfaceType)
        => interfaceType.GetMethods()
               .Any(m => m.GetCustomAttribute<ServiceCodeAttribute>() != null)
           || interfaceType.GetCustomAttribute<ServiceCodeAttribute>() != null;
}
