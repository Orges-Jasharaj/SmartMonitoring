using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Options;
using NotificationService.Features.SendAlert;
using SmartMonitoring.Shared.Messaging;
using SmartMonitoring.Shared.Notifications;

namespace NotificationService.Services;

public class KafkaAlertNotificationConsumerService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<KafkaAlertNotificationConsumerService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var options = kafkaOptions.Value;
        using var consumer = new ConsumerBuilder<string, string>(
            KafkaClientFactory.CreateConsumerConfig(options, options.AlertNotificationConsumerGroup)).Build();

        consumer.Subscribe(options.AlertNotificationTopic);
        logger.LogInformation(
            "Kafka alert notification consumer started on topic {Topic} (group {GroupId})",
            options.AlertNotificationTopic,
            options.AlertNotificationConsumerGroup);

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
                    logger.LogError(
                        ex,
                        "Kafka consume error on alert topic {Topic}",
                        options.AlertNotificationTopic);
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
            logger.LogInformation("Kafka alert notification consumer stopped");
        }
    }

    private void ProcessMessage(ConsumeResult<string, string> result, CancellationToken stoppingToken)
    {
        AlertNotificationRequest? notification;
        try
        {
            notification = JsonSerializer.Deserialize<AlertNotificationRequest>(
                result.Message.Value,
                KafkaJson.SerializerOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "Invalid alert notification at partition {Partition} offset {Offset}",
                result.Partition.Value,
                result.Offset.Value);
            return;
        }

        if (notification is null)
        {
            logger.LogWarning(
                "Empty alert notification at partition {Partition} offset {Offset}",
                result.Partition.Value,
                result.Offset.Value);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = mediator.Send(
                new SendAlertNotificationCommand { Notification = notification },
                stoppingToken)
            .GetAwaiter()
            .GetResult();

        if (!response.Success)
        {
            logger.LogError(
                "Failed to send alert {AlertId} from Kafka offset {Offset}: {Message}",
                notification.AlertId,
                result.Offset.Value,
                response.Message);
        }
    }
}
