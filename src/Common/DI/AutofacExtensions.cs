using Autofac;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Common.DI;

/// <summary>
/// Autofac 扩展方法：自动扫描实现了 IBaseAuto 的类并启用属性注入
/// </summary>
public static class AutofacExtensions
{
    /// <summary>
    /// 扫描程序集中所有实现了 IBaseAuto 的类，注册为自身并启用 PropertiesAutowired
    /// </summary>
    public static void RegisterByIBaseAuto(this ContainerBuilder builder, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var types = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IBaseAutofac).IsAssignableFrom(t));

        foreach (var type in types)
        {
            var registration = builder.RegisterType(type).AsSelf();
            registration = lifetime switch
            {
                ServiceLifetime.Singleton => registration.SingleInstance(),
                ServiceLifetime.Scoped => registration.InstancePerLifetimeScope(),
                _ => registration.InstancePerDependency()
            };
            registration.PropertiesAutowired();
        }
    }

    /// <summary>
    /// 扫描程序集中指定命名空间下所有实现了 IBaseAuto 的类
    /// </summary>
    public static void RegisterByIBaseAuto(this ContainerBuilder builder, Assembly assembly, string namespacePrefix, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var types = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, Namespace: not null }
                        && typeof(IBaseAutofac).IsAssignableFrom(t)
                        && t.Namespace!.StartsWith(namespacePrefix));

        foreach (var type in types)
        {
            var registration = builder.RegisterType(type).AsSelf();
            registration = lifetime switch
            {
                ServiceLifetime.Singleton => registration.SingleInstance(),
                ServiceLifetime.Scoped => registration.InstancePerLifetimeScope(),
                _ => registration.InstancePerDependency()
            };
            registration.PropertiesAutowired();
        }
    }
}
