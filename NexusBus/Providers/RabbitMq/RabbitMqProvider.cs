using NexusBus.Abstractions;

namespace NexusBus.Providers.RabbitMQ;

internal class RabbitMqProvider : IRabbitMqNexusBus
{
    private readonly RabbitMqProducer _producer;
    private readonly RabbitMqConsumer _consumer;

    public RabbitMqProvider(RabbitMqProducer producer, RabbitMqConsumer consumer)
    {
        _producer = producer;
        _consumer = consumer;
    }

    public Task PublishAsync<T>(string topicOrQueue, T message, CancellationToken cancellationToken = default)
    {
        return _producer.PublishAsync(topicOrQueue, message, cancellationToken);
    }

    public Task SubscribeAsync<T>(string topicOrQueue, Func<T, Task> handler, CancellationToken cancellationToken = default)
    {
        return _consumer.SubscribeAsync(topicOrQueue, handler, cancellationToken);
    }
}