namespace NexusBus.Configuration;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Exchange padrão para publicações. Se vazio, usa a default exchange ("")
    /// e mantém o comportamento atual (routingKey = nome da fila).
    /// </summary>
    public string ExchangeName { get; set; } = "nexusbus";

    /// <summary>
    /// Tipo da exchange (direct/topic/fanout/headers). Por padrão: direct.
    /// </summary>
    public string ExchangeType { get; set; } = "direct";

    /// <summary>
    /// Se true, declara exchange e faz bind da fila usando routing key.
    /// </summary>
    public bool DeclareTopology { get; set; } = true;

    /// <summary>
    /// Habilita dead-lettering via DLX (mensagens Nack/Requeue=false vão para DLQ).
    /// </summary>
    public bool EnableDeadLetter { get; set; } = false;

    /// <summary>
    /// Nome da exchange de dead-letter (direct). Usada quando EnableDeadLetter=true.
    /// </summary>
    public string DeadLetterExchangeName { get; set; } = "nexusbus.dlx";

    /// <summary>
    /// Sufixo da fila DLQ (ex: queue.pix.processar.dlq).
    /// </summary>
    public string DeadLetterQueueSuffix { get; set; } = ".dlq";
}