using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace SmartMonitoring.Shared.Messaging;

public static class KafkaClientFactory
{
    public static ProducerConfig CreateProducerConfig(KafkaOptions options) => new()
    {
        BootstrapServers = options.BootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true,
        MessageSendMaxRetries = 3,
    };

    public static ConsumerConfig CreateConsumerConfig(KafkaOptions options, string groupId) => new()
    {
        BootstrapServers = options.BootstrapServers,
        GroupId = groupId,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = true,
        EnablePartitionEof = true,
    };

    public static IProducer<string, string> CreateProducer(IOptions<KafkaOptions> options) =>
        new ProducerBuilder<string, string>(CreateProducerConfig(options.Value)).Build();
}
