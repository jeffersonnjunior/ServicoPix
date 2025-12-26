using NexusBus.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusBus.Configuration;
using ServicoPix.Domain.Interfaces.Services;

namespace ServicoPix.Infrastructure.Adapters;

public class NexusBusAdapter : IMensageriaService
{
    private readonly IRabbitMqNexusBus _rabbit;
    private readonly IKafkaNexusBus _kafka;
    private readonly ILogger<NexusBusAdapter> _logger;
    private readonly NexusOptions _nexusOptions;

    public NexusBusAdapter(
        IRabbitMqNexusBus rabbit,
        IKafkaNexusBus kafka,
        IOptions<NexusOptions> nexusOptions,
        ILogger<NexusBusAdapter> logger)
    {
        _rabbit = rabbit;
        _kafka = kafka;
        _logger = logger;
        _nexusOptions = nexusOptions.Value;
    }

    public async Task PublicarComandoAsync<T>(string fila, T mensagem)
    {
        var rmq = _nexusOptions.RabbitMq;
        _logger.LogInformation(
            "Mensageria[RabbitMQ]: Publicando comando (queue={Queue}, host={Host}:{Port}, vhost={VirtualHost}, payloadType={PayloadType})",
            fila,
            rmq.HostName,
            rmq.Port,
            rmq.VirtualHost ?? "/",
            typeof(T).Name);

        await _rabbit.PublishAsync(fila, mensagem);
    }

    public async Task PublicarEventoAsync<T>(string topico, T mensagem)
    {
        var kafka = _nexusOptions.Kafka;
        _logger.LogInformation(
            "Mensageria[Kafka]: Publicando evento (topic={Topic}, bootstrapServers={BootstrapServers}, groupId={GroupId}, payloadType={PayloadType})",
            topico,
            kafka.BootstrapServers,
            kafka.GroupId,
            typeof(T).Name);

        await _kafka.PublishAsync(topico, mensagem);
    }
}