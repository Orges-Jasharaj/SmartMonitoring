using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SmartMonitoring.Shared.Observability;

namespace SmartMonitoring.Shared.Middleware;

public class MetricsAuthorizationMiddleware(RequestDelegate next, IOptions<ObservabilityOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/metrics")
            && options.Value.RestrictMetricsToLocalhost)
        {
            var remote = context.Connection.RemoteIpAddress;
            if (remote is not null && !IPAddress.IsLoopback(remote))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        await next(context);
    }
}
