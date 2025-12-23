using NexusBus.Abstractions;
using ServicoPix.Domain.Interfaces.Services;

namespace ServicoPix.Infrastructure.Adapters;

public class NexusBusAdapter : IMensageriaService
{
    private readonly INexusBus _nexusClient;

    public NexusBusAdapter(INexusBus nexusClient)
    {
        _nexusClient = nexusClient;
    }

    public async Task PublicarComandoAsync<T>(string fila, T mensagem)
    {
        await _nexusClient.PublishAsync(fila, mensagem);
    }

    public async Task PublicarEventoAsync<T>(string topico, T mensagem)
    {
        //await _nexusClient.Stream.PublishAsync(topico, mensagem);
    }
}