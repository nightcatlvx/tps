namespace TpsApi.Extensions;

/// <summary>
/// TraceId 中间件：确保每个请求携带 TraceId 用于日志追踪
/// </summary>
public class TraceIdMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.Request.Headers["TraceId"].FirstOrDefault()
                      ?? Guid.NewGuid().ToString("N");

        if (string.IsNullOrEmpty(context.Request.Headers["TraceId"].FirstOrDefault()))
        {
            context.Request.Headers["TraceId"] = traceId;
        }

        context.Response.Headers["TraceId"] = traceId;

        await _next(context);
    }
}
