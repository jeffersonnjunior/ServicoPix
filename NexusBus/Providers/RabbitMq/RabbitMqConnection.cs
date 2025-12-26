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
    private readonly int _retryCount;
    private bool _disposed;

    public RabbitMqConnection(IOptions<NexusOptions> options, ILogger<RabbitMqConnection> logger)
    {
        _logger = logger;
        var config = options.Value.RabbitMq;
        _retryCount = Math.Max(0, config.RetryCount);

        _factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost ?? "/",
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            RequestedHeartbeat = TimeSpan.FromSeconds(30)
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

            Exception? lastException = null;
            var attempts = _retryCount + 1;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogInformation(
                        "NexusBus[RabbitMQ]: Conectando em {Host}:{Port} vhost={VirtualHost} (tentativa {Attempt}/{Total})",
                        _factory.HostName,
                        _factory.Port,
                        _factory.VirtualHost,
                        attempt,
                        attempts);

                    _connection = await _factory.CreateConnectionAsync(token);
                    return _connection;
                }
                catch (Exception ex) when (attempt < attempts)
                {
                    lastException = ex;
                    var delay = TimeSpan.FromSeconds(Math.Min(10, attempt));
                    _logger.LogWarning(
                        ex,
                        "NexusBus[RabbitMQ]: Falha ao conectar em {Host}:{Port} vhost={VirtualHost}. Nova tentativa em {Delay}.",
                        _factory.HostName,
                        _factory.Port,
                        _factory.VirtualHost,
                        delay);
                    await Task.Delay(delay, token);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    break;
                }
            }

            _logger.LogError(
                lastException,
                "NexusBus[RabbitMQ]: Falha fatal ao conectar em {Host}:{Port} vhost={VirtualHost}.",
                _factory.HostName,
                _factory.Port,
                _factory.VirtualHost);
            throw lastException ?? new InvalidOperationException("Falha fatal ao conectar no RabbitMQ.");
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
            _logger.LogInformation("NexusBus[RabbitMQ]: Conexão fechada.");
        }

        _lock.Dispose();
    }
}