using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusBus.Abstractions;
using Xunit;

namespace ServicoPix.Tests;

public class WorkerTests
{
    [Fact]
    public async Task Worker_consumes_rabbit_and_publishes_kafka_event()
    {
        var rabbit = new FakeRabbitBus();
        var kafka = new FakeKafkaBus();
        var logger = new TestLogger<ServicoPix.Worker.Worker>();

        var worker = new ServicoPix.Worker.Worker(rabbit, kafka, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var runTask = worker.StartAsync(cts.Token);

        // Wait for the rabbit subscription to be registered.
        await rabbit.Subscribed.Task.WaitAsync(cts.Token);

        // Create the private nested record: ProcessarPixMessage(Guid Id, JsonElement Dados)
        var workerType = worker.GetType();
        var msgType = workerType.GetNestedType("ProcessarPixMessage", BindingFlags.NonPublic);
        Assert.NotNull(msgType);

        var id = Guid.NewGuid();
        var payload = JsonDocument.Parse("{\"valor\": 123}").RootElement;
        var msg = Activator.CreateInstance(msgType!, id, payload);
        Assert.NotNull(msg);

        // Invoke the handler captured by the fake rabbit bus.
        await rabbit.Handler!(msg!);

        // Ensure Kafka publish happened.
        Assert.NotNull(kafka.LastPublished);
        Assert.Equal("topic.pix.processado", kafka.LastPublished.Value.Topic);

        // Validate event shape via reflection: PixProcessadoEvent(Guid Id, DateTimeOffset ProcessadoEm)
        var evtObj = kafka.LastPublished.Value.Message;
        var evtType = evtObj.GetType();
        var idProp = evtType.GetProperty("Id");
        Assert.NotNull(idProp);
        Assert.Equal(id, (Guid)idProp!.GetValue(evtObj)!);

        // Stop the worker.
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
        await runTask;
    }

    private sealed class FakeRabbitBus : IRabbitMqNexusBus
    {
        public TaskCompletionSource Subscribed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Func<object, Task>? Handler { get; private set; }

        public Task PublishAsync<T>(string topicOrQueue, T message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SubscribeAsync<T>(string topicOrQueue, Func<T, Task> handler, CancellationToken cancellationToken = default)
        {
            Handler = msg => handler((T)msg);
            Subscribed.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeKafkaBus : IKafkaNexusBus
    {
        public (string Topic, object Message)? LastPublished { get; private set; }

        public Task PublishAsync<T>(string topicOrQueue, T message, CancellationToken cancellationToken = default)
        {
            LastPublished = (topicOrQueue, message!);
            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T>(string topicOrQueue, Func<T, Task> handler, CancellationToken cancellationToken = default)
        {
            // Not required for this test.
            return Task.CompletedTask;
        }
    }
}
