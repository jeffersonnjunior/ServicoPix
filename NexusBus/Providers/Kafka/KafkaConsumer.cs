using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusBus.Configuration;

namespace NexusBus.Providers.Kafka;

internal sealed class KafkaConsumer
{
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly KafkaProducer _producer;
    private readonly NexusOptions _options;

    public KafkaConsumer(KafkaProducer producer, IOptions<NexusOptions> options, ILogger<KafkaConsumer> logger)
    {
        _producer = producer;
        _logger = logger;
        _options = options.Value;
    }

    public Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken token)
    {
        // Match RabbitMQ behavior: start consuming and return immediately.
        _ = Task.Run(() => ConsumeLoop(topic, handler, token), token);
        return Task.CompletedTask;
    }

    private async Task ConsumeLoop<T>(string topic, Func<T, Task> handler, CancellationToken token)
    {
        var consumerConfig = BuildConsumerConfig(_options.Kafka);

        using var consumer = new ConsumerBuilder<Ignore, byte[]>(consumerConfig)
            .SetValueDeserializer(Deserializers.ByteArray)
            .SetErrorHandler((_, e) => _logger.LogWarning("NexusBus: Kafka error: {Reason}", e.Reason))
            .Build();

        consumer.Subscribe(topic);

        _logger.LogInformation(
            "NexusBus: Ouvindo tópico Kafka {Topic} (groupId={GroupId})",
            topic,
            consumerConfig.GroupId);

        try
        {
            while (!token.IsCancellationRequested)
            {
                ConsumeResult<Ignore, byte[]>? cr;
                try
                {
                    cr = consumer.Consume(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (cr?.Message?.Value == null)
                    continue;

                var payload = cr.Message.Value;

                try
                {
                    var message = JsonSerializer.Deserialize<T>(payload);
                    if (message == null)
                        throw new JsonException("Mensagem desserializada é null");

                    await handler(message);

                    // Manual commit: prevents reprocessing when handler succeeds.
                    consumer.Commit(cr);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "NexusBus: Erro processando msg Kafka (topic={Topic}, partition={Partition}, offset={Offset})",
                        topic,
                        cr.Partition.Value,
                        cr.Offset.Value);

                    if (_options.Kafka.EnableDeadLetter)
                    {
                        var dlqTopic = topic + _options.Kafka.DeadLetterTopicSuffix;
                        try
                        {
                            // Forward the original payload to DLQ and commit to avoid poison-message loops.
                            await _producer.PublishRawAsync(dlqTopic, payload, cr.Message.Headers, token);
                            consumer.Commit(cr);
                        }
                        catch (Exception dlqEx)
                        {
                            _logger.LogWarning(dlqEx,
                                "NexusBus: Falha ao publicar em DLQ Kafka (dlqTopic={DlqTopic}). Offset não será commitado.",
                                dlqTopic);
                        }
                    }
                    // If DLQ is disabled, we intentionally do not commit; message may be retried.
                }
            }
        }
        finally
        {
            try
            {
                consumer.Close();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "NexusBus: Falha ao fechar consumer Kafka");
            }

            _logger.LogInformation("NexusBus: Consumer Kafka finalizado (topic={Topic})", topic);
        }
    }

    private static ConsumerConfig BuildConsumerConfig(KafkaOptions options)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
            GroupId = options.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = ParseAutoOffsetReset(options.AutoOffsetReset),
            EnablePartitionEof = false
        };

        KafkaProducer.ApplySecurity(config, options);
        return config;
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value)
    {
        if (Enum.TryParse<AutoOffsetReset>(value, ignoreCase: true, out var parsed))
            return parsed;

        return AutoOffsetReset.Earliest;
    }
}