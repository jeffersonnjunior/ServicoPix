using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusBus.Configuration;

namespace NexusBus.Providers.Kafka;

internal sealed class KafkaProducer : IDisposable
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly NexusOptions _options;
    private readonly IProducer<Null, byte[]> _producer;

    public KafkaProducer(IOptions<NexusOptions> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = BuildProducerConfig(_options.Kafka);
        _producer = new ProducerBuilder<Null, byte[]>(config)
            .SetValueSerializer(Serializers.ByteArray)
            .Build();
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var json = JsonSerializer.SerializeToUtf8Bytes(message);

        var headers = new Headers
        {
            { "content-type", System.Text.Encoding.UTF8.GetBytes("application/json") },
            { "message-id", System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")) }
        };

        var result = await _producer.ProduceAsync(
            topic,
            new Message<Null, byte[]> { Value = json, Headers = headers },
            token);

        _logger.LogDebug(
            "NexusBus: Mensagem publicada em Kafka (topic={Topic}, partition={Partition}, offset={Offset})",
            topic,
            result.Partition.Value,
            result.Offset.Value);
    }

    public async Task PublishRawAsync(string topic, byte[] payload, Headers? headers, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var msgHeaders = headers ?? new Headers();
        if (!ContainsHeader(msgHeaders, "content-type"))
            msgHeaders.Add("content-type", System.Text.Encoding.UTF8.GetBytes("application/json"));

        if (!ContainsHeader(msgHeaders, "message-id"))
            msgHeaders.Add("message-id", System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")));

        await _producer.ProduceAsync(
            topic,
            new Message<Null, byte[]> { Value = payload, Headers = msgHeaders },
            token);
    }

    private static ProducerConfig BuildProducerConfig(KafkaOptions options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        ApplySecurity(config, options);
        return config;
    }

    internal static void ApplySecurity(ClientConfig config, KafkaOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SecurityProtocol) &&
            Enum.TryParse<SecurityProtocol>(options.SecurityProtocol, ignoreCase: true, out var securityProtocol))
        {
            config.SecurityProtocol = securityProtocol;
        }

        if (!string.IsNullOrWhiteSpace(options.SaslMechanism) &&
            Enum.TryParse<SaslMechanism>(options.SaslMechanism, ignoreCase: true, out var saslMechanism))
        {
            config.SaslMechanism = saslMechanism;
        }

        if (!string.IsNullOrWhiteSpace(options.SaslUsername))
            config.SaslUsername = options.SaslUsername;

        if (!string.IsNullOrWhiteSpace(options.SaslPassword))
            config.SaslPassword = options.SaslPassword;
    }

    private static bool ContainsHeader(Headers headers, string key)
    {
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public void Dispose()
    {
        try
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // ignore flush failures
        }
        finally
        {
            _producer.Dispose();
        }
    }
}