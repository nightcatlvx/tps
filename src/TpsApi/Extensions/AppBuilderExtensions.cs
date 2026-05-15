namespace TpsApi.Extensions;

/// <summary>
/// 应用中间件管道扩展
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// 添加 TraceId 中间件
    /// </summary>
    public static IApplicationBuilder UseTpsMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<TraceIdMiddleware>();
        return app;
    }
}
