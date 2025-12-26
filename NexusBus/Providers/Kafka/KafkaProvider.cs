using NexusBus.Abstractions;

namespace NexusBus.Providers.Kafka;

internal class KafkaProvider : INexusBus
{
    private readonly KafkaProducer _producer;
    private readonly KafkaConsumer _consumer;

    public KafkaProvider(KafkaProducer producer, KafkaConsumer consumer)
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