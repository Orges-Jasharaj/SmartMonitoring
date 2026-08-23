using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SmartMonitoring.Shared.Notifications;

public static class NotificationServiceExtensions
{
    public static IServiceCollection AddNotificationPublishing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));

        services.AddHttpClient<INotificationPublisher, HttpNotificationPublisher>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<NotificationOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Notification-Api-Key", options.ApiKey);
            }
        });

        return services;
    }
}
