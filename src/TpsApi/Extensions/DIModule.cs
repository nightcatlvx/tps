using Autofac;
using Common.DI;
using Microsoft.AspNetCore.Mvc;
using TpsApi.Client.Auth;
using TpsApi.Client.Sdk;
using TpsApi.Client.Signing;
using System.Reflection;

namespace TpsApi.Extensions;

public class DIModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();

        builder.RegisterByIBaseAuto(assembly, "TpsApi.Services");
        builder.RegisterByIBaseAuto(assembly, "TpsApi.Repositories");

        builder.RegisterAssemblyTypes(assembly)
            .Where(t => typeof(IRequestSigner).IsAssignableFrom(t) && !t.IsAbstract)
            .As<IRequestSigner>().InstancePerDependency();

        builder.RegisterAssemblyTypes(assembly)
            .Where(t => typeof(IServiceAuth).IsAssignableFrom(t) && !t.IsAbstract)
            .As<IServiceAuth>().InstancePerDependency();

        builder.RegisterAssemblyTypes(assembly)
            .Where(t => typeof(ISdkAdapter).IsAssignableFrom(t) && !t.IsAbstract)
            .AsSelf().InstancePerDependency();

        builder.RegisterAssemblyTypes(assembly)
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
            .PropertiesAutowired();
    }
}
