namespace NexusBus.Configuration;

public class NexusOptions
{
    public const string SectionName = "NexusBus";
    public string Provider { get; set; } = "RabbitMQ";
    public RabbitMqOptions RabbitMq { get; set; } = new();
    public KafkaOptions Kafka { get; set; } = new();
}
