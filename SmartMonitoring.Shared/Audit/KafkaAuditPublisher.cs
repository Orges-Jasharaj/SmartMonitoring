using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMonitoring.Shared.Messaging;

namespace SmartMonitoring.Shared.Audit;

public class KafkaAuditPublisher(
    IProducer<string, string> producer,
    IOptions<KafkaOptions> kafkaOptions,
    IOptions<AuditOptions> auditOptions,
    ILogger<KafkaAuditPublisher> logger) : IAuditPublisher
{
    public async Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        if (!auditOptions.Value.Enabled || !kafkaOptions.Value.Enabled)
        {
            return;
        }

        var topic = kafkaOptions.Value.AuditTopic;
        var key = auditEvent.CorrelationId
            ?? auditEvent.TargetEntityId
            ?? Guid.NewGuid().ToString("N");

        var payload = JsonSerializer.Serialize(auditEvent, KafkaJson.SerializerOptions);

        try
        {
            var result = await producer.ProduceAsync(
                topic,
                new Message<string, string> { Key = key, Value = payload },
                cancellationToken);

            logger.LogDebug(
                "Published audit event {EventType} to {Topic} partition {Partition} offset {Offset}",
                auditEvent.EventType,
                topic,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            logger.LogError(
                ex,
                "Failed to publish audit event {EventType} to Kafka topic {Topic}",
                auditEvent.EventType,
                topic);
            throw;
        }
    }
}
