using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusBus.Configuration;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NexusBus.Providers.RabbitMQ;

internal class RabbitMqConsumer
{
    private readonly RabbitMqConnection _connectionManager;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly NexusOptions _options;

    public RabbitMqConsumer(RabbitMqConnection connectionManager, IOptions<NexusOptions> options, ILogger<RabbitMqConsumer> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _options = options.Value;

        var retryCount = _options.RabbitMq.RetryCount;
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(retryCount, _ => TimeSpan.FromSeconds(1));
    }

    public async Task SubscribeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken token)
    {
        var connection = await _connectionManager.GetConnectionAsync(token);
        var channel = await connection.CreateChannelAsync(cancellationToken: token);

        var rmq = _options.RabbitMq;
        var requeueOnError = !rmq.EnableDeadLetter;
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
            await channel.QueueDeclareAsync(queue: dlqName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: token);
            await channel.QueueBindAsync(queue: dlqName, exchange: rmq.DeadLetterExchangeName, routingKey: dlqName, arguments: null, cancellationToken: token);
        }

        await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs, cancellationToken: token);

        if (rmq.DeclareTopology && useCustomExchange)
        {
            await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: queueName, arguments: null, cancellationToken: token);
        }

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: token);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json);

                if (message == null)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: requeueOnError, cancellationToken: token);
                    return;
                }

                await _retryPolicy.ExecuteAsync(async () => await handler(message));

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: token);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "NexusBus[RabbitMQ]: Erro processando msg (queue={Queue}, host={Host}:{Port}, vhost={VirtualHost})",
                    queueName,
                    rmq.HostName,
                    rmq.Port,
                    rmq.VirtualHost ?? "/");

                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: requeueOnError, cancellationToken: token);
            }
        };

        var consumerTag = await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: token);

        _logger.LogInformation(
            "NexusBus[RabbitMQ]: Ouvindo fila {Queue} (consumer={ConsumerTag}, host={Host}:{Port}, vhost={VirtualHost})",
            queueName,
            consumerTag,
            rmq.HostName,
            rmq.Port,
            rmq.VirtualHost ?? "/");

           if (token.CanBeCanceled)
        {
            token.Register(() =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation(
                            "NexusBus[RabbitMQ]: Cancelando consumer {ConsumerTag} (queue={Queue})",
                            consumerTag,
                            queueName);

                        try
                        {
                            await channel.BasicCancelAsync(consumerTag);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Falha ao executar BasicCancelAsync para {Queue}", queueName);
                        }

                        try
                        {
                            await channel.CloseAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Falha ao fechar o canal (CloseAsync) para {Queue}", queueName);
                        }

                        try
                        {
                            await channel.DisposeAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Falha ao descartar o canal (DisposeAsync) para {Queue}", queueName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Erro na tarefa de cancelamento do consumer para {Queue}", queueName);
                    }
                });
            });
        }
    }
}