using Microsoft.Extensions.Options;
using Moq;
using NexusBus.Abstractions;
using NexusBus.Configuration;
using ServicoPix.Infrastructure.Adapters;
using Xunit;

namespace ServicoPix.Tests;

public class NexusBusAdapterTests
{
    private sealed record TestCommand(Guid Id);
    private sealed record TestEvent(Guid Id);

    [Fact]
    public async Task PublicarComandoAsync_publishes_to_rabbit_and_logs_endpoint()
    {
        var rabbit = new Mock<IRabbitMqNexusBus>(MockBehavior.Strict);
        var kafka = new Mock<IKafkaNexusBus>(MockBehavior.Strict);

        var cmd = new TestCommand(Guid.NewGuid());

        rabbit
            .Setup(x => x.PublishAsync("queue.teste", cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = Options.Create(new NexusOptions
        {
            RabbitMq = new RabbitMqOptions
            {
                HostName = "rabbitmq",
                Port = 5672,
                VirtualHost = "/"
            },
            Kafka = new KafkaOptions
            {
                BootstrapServers = "kafka:9092",
                GroupId = "nexusbus"
            }
        });

        var logger = new TestLogger<NexusBusAdapter>();

        var adapter = new NexusBusAdapter(rabbit.Object, kafka.Object, options, logger);

        await adapter.PublicarComandoAsync("queue.teste", cmd);

        rabbit.VerifyAll();
        kafka.VerifyNoOtherCalls();

        var entry = Assert.Single(logger.Entries.Where(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Information));
        Assert.Contains("Mensageria[RabbitMQ]: Publicando comando", entry.Message);

        Assert.Contains(entry.State, kv => kv.Key == "Queue" && (string?)kv.Value == "queue.teste");
        Assert.Contains(entry.State, kv => kv.Key == "Host" && (string?)kv.Value == "rabbitmq");
        Assert.Contains(entry.State, kv => kv.Key == "Port" && (int?)kv.Value == 5672);
        Assert.Contains(entry.State, kv => kv.Key == "VirtualHost" && (string?)kv.Value == "/");
        Assert.Contains(entry.State, kv => kv.Key == "PayloadType" && (string?)kv.Value == nameof(TestCommand));
    }

    [Fact]
    public async Task PublicarEventoAsync_publishes_to_kafka_and_logs_endpoint()
    {
        var rabbit = new Mock<IRabbitMqNexusBus>(MockBehavior.Strict);
        var kafka = new Mock<IKafkaNexusBus>(MockBehavior.Strict);

        var evt = new TestEvent(Guid.NewGuid());

        kafka
            .Setup(x => x.PublishAsync("topic.teste", evt, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = Options.Create(new NexusOptions
        {
            RabbitMq = new RabbitMqOptions
            {
                HostName = "rabbitmq",
                Port = 5672,
                VirtualHost = "/"
            },
            Kafka = new KafkaOptions
            {
                BootstrapServers = "kafka:9092",
                GroupId = "grupo-teste"
            }
        });

        var logger = new TestLogger<NexusBusAdapter>();

        var adapter = new NexusBusAdapter(rabbit.Object, kafka.Object, options, logger);

        await adapter.PublicarEventoAsync("topic.teste", evt);

        kafka.VerifyAll();
        rabbit.VerifyNoOtherCalls();

        var entry = Assert.Single(logger.Entries.Where(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Information));
        Assert.Contains("Mensageria[Kafka]: Publicando evento", entry.Message);

        Assert.Contains(entry.State, kv => kv.Key == "Topic" && (string?)kv.Value == "topic.teste");
        Assert.Contains(entry.State, kv => kv.Key == "BootstrapServers" && (string?)kv.Value == "kafka:9092");
        Assert.Contains(entry.State, kv => kv.Key == "GroupId" && (string?)kv.Value == "grupo-teste");
        Assert.Contains(entry.State, kv => kv.Key == "PayloadType" && (string?)kv.Value == nameof(TestEvent));
    }
}
