using Microsoft.Extensions.Options;

namespace NotificationService.Middleware;

public class NotificationApiKeyMiddleware(RequestDelegate next, IOptions<NotificationApiKeyOptions> options)
{
    public const string HeaderName = "X-Notification-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.StartsWithSegments("/api/notifications"))
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

public class NotificationApiKeyOptions
{
    public const string SectionName = "NotificationApiKey";
    public string? ApiKey { get; set; }
}

public static class NotificationApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseNotificationApiKey(this IApplicationBuilder app)
        => app.UseMiddleware<NotificationApiKeyMiddleware>();
}
