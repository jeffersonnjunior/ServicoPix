using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusBus.Configuration;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;

namespace NexusBus.Providers.RabbitMQ;

internal class RabbitMqProducer
{
    private readonly RabbitMqConnection _connectionManager;
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public RabbitMqProducer(RabbitMqConnection connectionManager, IOptions<NexusOptions> options, ILogger<RabbitMqProducer> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;

        var retryCount = options.Value.RabbitMq.RetryCount;
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(retryCount, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    public Task PublishAsync<T>(string queueName, T message, CancellationToken token)
    {
        return _retryPolicy.ExecuteAsync(async () =>
        {
            var connection = await _connectionManager.GetConnectionAsync(token);

            await using var channel = await connection.CreateChannelAsync(cancellationToken: token);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: token);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: token);

            _logger.LogDebug("Mensagem enviada para fila {Queue} (RabbitMQ v7)", queueName);
        });
    }
}