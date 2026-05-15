using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Data;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using TpsApi.Extensions;

var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Autofac
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        containerBuilder.RegisterModule<DIModule>();
    });

    // Mvc
    builder.Services.Configure<ApiBehaviorOptions>(options =>
        options.SuppressModelStateInvalidFilter = true);

    builder.Services.AddHttpClient();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddControllers()
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        })
        .AddControllersAsServices();

    // DB + Redis 统一初始化（读 appsettings.json）
    builder.Services.AddTpsInfrastructure(builder.Configuration);

    // 第三方服务代理
    builder.Services.AddThirdPartyClients();

    var app = builder.Build();

    app.UseMiddleware<TraceIdMiddleware>();
    app.UseRouting();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "程序启动失败");
    throw;
}
finally
{
    LogManager.Shutdown();
}
