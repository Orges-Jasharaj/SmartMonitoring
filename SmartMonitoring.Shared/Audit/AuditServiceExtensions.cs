using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SmartMonitoring.Shared.Audit;

public static class AuditServiceExtensions
{
    public static IServiceCollection AddAuditPublishing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuditOptions>(configuration.GetSection(AuditOptions.SectionName));

        services.PostConfigure<AuditOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ServiceName) || options.ServiceName == "UnknownService")
            {
                options.ServiceName = AppDomain.CurrentDomain.FriendlyName;
            }
        });

        services.AddHttpContextAccessor();
        services.AddHttpClient<IAuditPublisher, HttpAuditPublisher>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AuditOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Audit-Api-Key", options.ApiKey);
            }
        });

        services.AddScoped<IAuditRecorder, AuditRecorder>();

        return services;
    }
}
