using System.Text.Json;
using AuditLogging.Features.RecordAudit;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Options;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Messaging;

namespace AuditLogging.Services;

public class KafkaAuditConsumerService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<KafkaAuditConsumerService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var options = kafkaOptions.Value;
        using var consumer = new ConsumerBuilder<string, string>(
            KafkaClientFactory.CreateConsumerConfig(options, options.AuditConsumerGroup)).Build();

        consumer.Subscribe(options.AuditTopic);
        logger.LogInformation(
            "Kafka audit consumer started on topic {Topic} (group {GroupId})",
            options.AuditTopic,
            options.AuditConsumerGroup);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result.IsPartitionEOF)
                    {
                        continue;
                    }

                    ProcessMessage(result, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error on audit topic {Topic}", options.AuditTopic);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            consumer.Close();
            logger.LogInformation("Kafka audit consumer stopped");
        }
    }

    private void ProcessMessage(ConsumeResult<string, string> result, CancellationToken stoppingToken)
    {
        AuditEvent? auditEvent;
        try
        {
            auditEvent = JsonSerializer.Deserialize<AuditEvent>(result.Message.Value, KafkaJson.SerializerOptions);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            logger.LogError(
                ex,
                "Invalid audit message at partition {Partition} offset {Offset}",
                result.Partition.Value,
                result.Offset.Value);
            return;
        }

        if (auditEvent is null)
        {
            logger.LogWarning(
                "Empty audit message at partition {Partition} offset {Offset}",
                result.Partition.Value,
                result.Offset.Value);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = mediator.Send(new RecordAuditCommand { AuditEvent = auditEvent }, stoppingToken)
            .GetAwaiter()
            .GetResult();

        if (!response.Success)
        {
            logger.LogError(
                "Failed to persist audit event {EventType} from Kafka offset {Offset}: {Message}",
                auditEvent.EventType,
                result.Offset.Value,
                response.Message);
        }
    }
}
