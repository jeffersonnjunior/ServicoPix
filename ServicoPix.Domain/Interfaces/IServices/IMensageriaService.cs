namespace ServicoPix.Domain.Interfaces.Services;

public interface IMensageriaService
{
    Task PublicarComandoAsync<T>(string fila, T mensagem);
    Task PublicarEventoAsync<T>(string topico, T mensagem);
}