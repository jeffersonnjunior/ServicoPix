using System.Text.Json;
using NexusBus.Abstractions;

namespace ServicoPix.Worker
{
    public class Worker(INexusBus bus, ILogger<Worker> logger) : BackgroundService
    {
        private sealed record ProcessarPixMessage(Guid Id, JsonElement Dados);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Worker iniciando consumo da fila {Queue}", "queue.pix.processar");

            await bus.SubscribeAsync<ProcessarPixMessage>(
                "queue.pix.processar",
                async message =>
                {
                    logger.LogInformation(
                        "Mensagem recebida (queue.pix.processar): Id={Id} Dados={Dados}",
                        message.Id,
                        message.Dados.GetRawText());

                    await Task.CompletedTask;
                },
                stoppingToken);

            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
    }
}
