using NexusBus.Abstractions;
using ServicoPix.Domain.Interfaces.Services;

namespace ServicoPix.Infrastructure.Adapters;

public class NexusBusAdapter : IMensageriaService
{
    private readonly IRabbitMqNexusBus _rabbit;
    private readonly IKafkaNexusBus _kafka;

    public NexusBusAdapter(IRabbitMqNexusBus rabbit, IKafkaNexusBus kafka)
    {
        _rabbit = rabbit;
        _kafka = kafka;
    }

    public async Task PublicarComandoAsync<T>(string fila, T mensagem)
    {
        await _rabbit.PublishAsync(fila, mensagem);
    }

    public async Task PublicarEventoAsync<T>(string topico, T mensagem)
    {
        await _kafka.PublishAsync(topico, mensagem);
    }
}