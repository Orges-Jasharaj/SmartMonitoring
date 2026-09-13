using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace SmartMonitoring.Shared.Messaging;

public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaProducer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        var kafkaOptions = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
        if (!kafkaOptions.Enabled)
        {
            return services;
        }

        services.TryAddSingleton<IProducer<string, string>>(sp =>
            KafkaClientFactory.CreateProducer(sp.GetRequiredService<IOptions<KafkaOptions>>()));

        return services;
    }

    public static bool UseKafkaTransport(IConfiguration configuration, string sectionName)
    {
        var kafkaEnabled = configuration.GetValue<bool>($"{KafkaOptions.SectionName}:Enabled");
        var transport = configuration.GetValue<string>($"{sectionName}:Transport") ?? MessagingTransport.Http;
        return kafkaEnabled && string.Equals(transport, MessagingTransport.Kafka, StringComparison.OrdinalIgnoreCase);
    }
}
