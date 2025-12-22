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

    public RabbitMqConsumer(RabbitMqConnection connectionManager, IOptions<NexusOptions> options, ILogger<RabbitMqConsumer> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;

        var retryCount = options.Value.RabbitMq.RetryCount;
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(retryCount, _ => TimeSpan.FromSeconds(1));
    }

    public async Task SubscribeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken token)
    {
        var connection = await _connectionManager.GetConnectionAsync(token);
        var channel = await connection.CreateChannelAsync(cancellationToken: token);

        await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: token);
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
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: token);
                    return;
                }

                await _retryPolicy.ExecuteAsync(async () => await handler(message));

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro processando msg na fila {Queue}", queueName);

                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: token);
            }
        };

        var consumerTag = await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: token);

        _logger.LogInformation("NexusBus: Ouvindo fila {Queue} com RabbitMQ v7 (consumer={ConsumerTag})", queueName, consumerTag);

             if (token.CanBeCanceled)
        {
            token.Register(() =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("NexusBus: Cancelando consumer {ConsumerTag} da fila {Queue}", consumerTag, queueName);

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