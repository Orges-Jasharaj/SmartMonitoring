using Microsoft.Extensions.Options;

namespace AuditLogging.Middleware;

public class AuditApiKeyMiddleware(RequestDelegate next, IOptions<AuditApiKeyOptions> options)
{
    public const string HeaderName = "X-Audit-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.StartsWithSegments("/api/audit/events"))
        {
            var configuredKey = options.Value.ApiKey;
            if (!string.IsNullOrWhiteSpace(configuredKey))
            {
                if (!context.Request.Headers.TryGetValue(HeaderName, out var providedKey)
                    || providedKey != configuredKey)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
        }

        await next(context);
    }
}

public class AuditApiKeyOptions
{
    public const string SectionName = "AuditApiKey";
    public string? ApiKey { get; set; }
}

public static class AuditApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditApiKey(this IApplicationBuilder app)
        => app.UseMiddleware<AuditApiKeyMiddleware>();
}
