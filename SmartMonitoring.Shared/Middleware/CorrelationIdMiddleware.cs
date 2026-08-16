using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace SmartMonitoring.Shared.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        var activity = Activity.Current;
        var traceId = activity?.TraceId.ToString();
        var spanId = activity?.SpanId.ToString();

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", traceId ?? string.Empty))
        using (LogContext.PushProperty("SpanId", spanId ?? string.Empty))
        {
            await next(context);
        }
    }
}
