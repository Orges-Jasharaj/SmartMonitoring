namespace SmartMonitoring.Shared.Messaging;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; }

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string AuditTopic { get; set; } = KafkaTopics.AuditEvents;

    public string AlertNotificationTopic { get; set; } = KafkaTopics.AlertNotifications;

    public string AuditConsumerGroup { get; set; } = "audit-logging";

    public string AlertNotificationConsumerGroup { get; set; } = "notification-service";
}

public static class KafkaTopics
{
    public const string AuditEvents = "smartmonitoring.audit.events";

    public const string AlertNotifications = "smartmonitoring.alerts.notifications";
}

public static class MessagingTransport
{
    public const string Http = "Http";

    public const string Kafka = "Kafka";
}
