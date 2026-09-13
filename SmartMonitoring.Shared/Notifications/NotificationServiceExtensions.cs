using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartMonitoring.Shared.Messaging;

namespace SmartMonitoring.Shared.Notifications;

public static class NotificationServiceExtensions
{
    public static IServiceCollection AddNotificationPublishing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));

        if (KafkaServiceCollectionExtensions.UseKafkaTransport(configuration, NotificationOptions.SectionName))
        {
            services.AddKafkaProducer(configuration);
            services.AddSingleton<INotificationPublisher, KafkaNotificationPublisher>();
        }
        else
        {
            services.AddHttpClient<INotificationPublisher, HttpNotificationPublisher>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<NotificationOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    client.DefaultRequestHeaders.Add("X-Notification-Api-Key", options.ApiKey);
                }
            });
        }

        return services;
    }
}
