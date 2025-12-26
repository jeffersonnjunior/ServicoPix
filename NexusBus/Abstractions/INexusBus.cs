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

/// <summary>
/// Abstração tipada para RabbitMQ.
/// Permite ao consumidor injetar e usar RabbitMQ e Kafka ao mesmo tempo no mesmo projeto.
/// </summary>
public interface IRabbitMqNexusBus : INexusBus;

/// <summary>
/// Abstração tipada para Kafka.
/// Permite ao consumidor injetar e usar RabbitMQ e Kafka ao mesmo tempo no mesmo projeto.
/// </summary>
public interface IKafkaNexusBus : INexusBus;
