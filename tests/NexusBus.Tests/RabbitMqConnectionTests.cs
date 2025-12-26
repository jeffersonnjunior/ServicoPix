using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusBus.Configuration;
using NexusBus.Providers.RabbitMQ;
using Xunit;

namespace NexusBus.Tests;

public class RabbitMqConnectionTests
{
    [Fact]
    public async Task GetConnectionAsync_throws_if_token_is_already_cancelled()
    {
        var options = Options.Create(new NexusOptions
        {
            RabbitMq = new RabbitMqOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                RetryCount = 0
            }
        });

        await using var conn = new RabbitMqConnection(options, NullLogger<RabbitMqConnection>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => conn.GetConnectionAsync(cts.Token));
    }
}
