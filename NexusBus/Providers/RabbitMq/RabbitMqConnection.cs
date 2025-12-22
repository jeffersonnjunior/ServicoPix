using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusBus.Configuration;
using RabbitMQ.Client;

namespace NexusBus.Providers.RabbitMQ;

internal class RabbitMqConnection : IAsyncDisposable
{
    private IConnection? _connection;
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public RabbitMqConnection(IOptions<NexusOptions> options, ILogger<RabbitMqConnection> logger)
    {
        _logger = logger;
        var config = options.Value.RabbitMq;

        _factory = new ConnectionFactory
        {
            HostName = config.HostName,
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost ?? "/"
        };
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken token = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _lock.WaitAsync(token);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _logger.LogInformation("NexusBus: Iniciando conexão assíncrona com RabbitMQ v7...");
            _connection = await _factory.CreateConnectionAsync(token);
            return _connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NexusBus: Falha fatal ao conectar no RabbitMQ.");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _logger.LogInformation("NexusBus: Conexão RabbitMQ fechada.");
        }

        _lock.Dispose();
    }
}