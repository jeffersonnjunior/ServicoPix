namespace NexusBus.Abstractions;

public interface INexusBus
{
    /// <summary>
    /// Publica uma mensagem para uma fila (Rabbit) ou tópico (Kafka).
    /// </summary>
    Task PublishAsync<T>(string topicOrQueue, T message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra um processador para ouvir mensagens.
    /// A lib gerencia a conexão e o loop de consumo internamente.
    /// </summary>
    Task SubscribeAsync<T>(string topicOrQueue, Func<T, Task> handler, CancellationToken cancellationToken = default);
}
