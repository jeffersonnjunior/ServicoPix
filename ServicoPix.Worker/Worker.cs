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

            var attempt = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
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

                    logger.LogInformation("Consumer registrado; aguardando mensagens...");
                    attempt = 0;
                    await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    attempt++;

                    var baseDelaySeconds = Math.Min(30, Math.Pow(2, Math.Min(attempt, 5))); 
                    var jitterMs = Random.Shared.Next(0, 250);
                    var delay = TimeSpan.FromSeconds(baseDelaySeconds) + TimeSpan.FromMilliseconds(jitterMs);

                    logger.LogWarning(ex, "Falha ao iniciar consumo (tentativa {Attempt}); tentando novamente em {Delay}...", attempt, delay);
                    await Task.Delay(delay, stoppingToken);
                }
            }
        }
    }
}
