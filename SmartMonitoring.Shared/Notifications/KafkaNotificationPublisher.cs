using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMonitoring.Shared.Messaging;

namespace SmartMonitoring.Shared.Notifications;

public class KafkaNotificationPublisher(
    IProducer<string, string> producer,
    IOptions<KafkaOptions> kafkaOptions,
    IOptions<NotificationOptions> notificationOptions,
    ILogger<KafkaNotificationPublisher> logger) : INotificationPublisher
{
    public async Task PublishAlertAsync(AlertNotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (!notificationOptions.Value.Enabled || !kafkaOptions.Value.Enabled)
        {
            return;
        }

        var topic = kafkaOptions.Value.AlertNotificationTopic;
        var key = request.AlertId.ToString("D");
        var payload = JsonSerializer.Serialize(request, KafkaJson.SerializerOptions);

        try
        {
            var result = await producer.ProduceAsync(
                topic,
                new Message<string, string> { Key = key, Value = payload },
                cancellationToken);

            logger.LogDebug(
                "Published alert notification {AlertId} to {Topic} partition {Partition} offset {Offset}",
                request.AlertId,
                topic,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            logger.LogError(
                ex,
                "Failed to publish alert notification {AlertId} to Kafka topic {Topic}",
                request.AlertId,
                topic);
            throw;
        }
    }
}
