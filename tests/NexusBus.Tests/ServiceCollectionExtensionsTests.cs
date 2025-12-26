using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusBus.Abstractions;
using NexusBus.Extensions;
using Xunit;

namespace NexusBus.Tests;

public class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("Kafka", typeof(IKafkaNexusBus))]
    [InlineData("RabbitMQ", typeof(IRabbitMqNexusBus))]
    [InlineData("rabbitmq", typeof(IRabbitMqNexusBus))]
    [InlineData(null, typeof(IRabbitMqNexusBus))]
    [InlineData("", typeof(IRabbitMqNexusBus))]
    public async Task AddNexusBus_resolves_INexusBus_based_on_Provider(string? provider, Type expectedInterface)
    {
        var settings = new Dictionary<string, string?>
        {
            ["NexusBus:Provider"] = provider,
            ["NexusBus:RabbitMq:HostName"] = "localhost",
            ["NexusBus:Kafka:BootstrapServers"] = "localhost:9092",
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNexusBus(config);

        await using var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<IRabbitMqNexusBus>());
        Assert.NotNull(sp.GetRequiredService<IKafkaNexusBus>());

        var bus = sp.GetRequiredService<INexusBus>();
        Assert.IsAssignableFrom(expectedInterface, bus);
    }
}
