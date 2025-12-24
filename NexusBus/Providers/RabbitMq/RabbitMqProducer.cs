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
    private readonly NexusOptions _options;

    public RabbitMqProducer(RabbitMqConnection connectionManager, IOptions<NexusOptions> options, ILogger<RabbitMqProducer> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _options = options.Value;

        var retryCount = _options.RabbitMq.RetryCount;
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

            var rmq = _options.RabbitMq;
            var useCustomExchange = rmq.DeclareTopology && !string.IsNullOrWhiteSpace(rmq.ExchangeName);
            var exchangeName = useCustomExchange ? rmq.ExchangeName : "";

            if (rmq.DeclareTopology && useCustomExchange)
            {
                await channel.ExchangeDeclareAsync(
                    exchange: exchangeName,
                    type: rmq.ExchangeType,
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: token);
            }

            IDictionary<string, object?>? queueArgs = null;
            if (rmq.EnableDeadLetter)
            {
                queueArgs = new Dictionary<string, object?>
                {
                    ["x-dead-letter-exchange"] = rmq.DeadLetterExchangeName,
                    ["x-dead-letter-routing-key"] = queueName + rmq.DeadLetterQueueSuffix
                };

                await channel.ExchangeDeclareAsync(
                    exchange: rmq.DeadLetterExchangeName,
                    type: "direct",
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: token);

                var dlqName = queueName + rmq.DeadLetterQueueSuffix;
                await channel.QueueDeclareAsync(
                    queue: dlqName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: token);

                await channel.QueueBindAsync(
                    queue: dlqName,
                    exchange: rmq.DeadLetterExchangeName,
                    routingKey: dlqName,
                    arguments: null,
                    cancellationToken: token);
            }

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: token);

            if (rmq.DeclareTopology && useCustomExchange)
            {
                await channel.QueueBindAsync(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: queueName,
                    arguments: null,
                    cancellationToken: token);
            }

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString("N")
            };

            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: queueName,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: token);

            _logger.LogDebug("Mensagem enviada para fila {Queue} (RabbitMQ v7)", queueName);
        });
    }
}