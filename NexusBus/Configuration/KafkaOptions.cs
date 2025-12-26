namespace NexusBus.Configuration;

public class KafkaOptions
{
    /// <summary>
    /// Lista de bootstrap servers (ex: "localhost:9092").
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// ClientId do producer/consumer.
    /// </summary>
    public string ClientId { get; set; } = "nexusbus";

    /// <summary>
    /// GroupId padrão para consumidores.
    /// Pode ser sobrescrito por subscription (via configuração do consumidor).
    /// </summary>
    public string GroupId { get; set; } = "nexusbus";

    /// <summary>
    /// Se true, habilita publicação para tópico DLQ (topic + DeadLetterTopicSuffix)
    /// quando o handler falha.
    /// </summary>
    public bool EnableDeadLetter { get; set; } = false;

    /// <summary>
    /// Sufixo do tópico DLQ (ex: ".dlq" => "pix.processar.dlq").
    /// </summary>
    public string DeadLetterTopicSuffix { get; set; } = ".dlq";

    /// <summary>
    /// AutoOffsetReset: "Earliest" ou "Latest".
    /// </summary>
    public string AutoOffsetReset { get; set; } = "Earliest";

    /// <summary>
    /// Segurança (opcional): "Plaintext", "Ssl", "SaslPlaintext", "SaslSsl".
    /// </summary>
    public string? SecurityProtocol { get; set; }

    /// <summary>
    /// SASL (opcional): "Plain", "ScramSha256", "ScramSha512".
    /// </summary>
    public string? SaslMechanism { get; set; }

    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
}