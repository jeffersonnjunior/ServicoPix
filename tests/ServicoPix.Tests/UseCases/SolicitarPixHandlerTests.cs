using Moq;
using Moq.Language.Flow;
using ServicoPix.Application.UseCases.SolicitarPix.Commands;
using ServicoPix.Domain.Interfaces.Services;
using Xunit;

namespace ServicoPix.Tests.UseCases;

public class SolicitarPixHandlerTests
{
    [Fact]
    public async Task Handle_publishes_command_to_rabbit_queue_and_returns_protocol()
    {
        var mensageria = new Mock<IMensageriaService>(MockBehavior.Strict);

        string? capturedQueue = null;
        object? capturedMessage = null;

        mensageria
            .Setup(m => m.PublicarComandoAsync(It.IsAny<string>(), It.IsAny<It.IsAnyType>()))
            .Returns(Task.CompletedTask)
            .Callback(new InvocationAction(invocation =>
            {
                capturedQueue = (string)invocation.Arguments[0];
                capturedMessage = invocation.Arguments[1];
            }));

        var handler = new SolicitarPixHandler(mensageria.Object);

        var request = new SolicitarPixCommand
        {
            ContaOrigemId = Guid.NewGuid(),
            ContaDestinoId = Guid.NewGuid(),
            Valor = 123m
        };

        var protocolo = await handler.Handle(request, CancellationToken.None);

        mensageria.Verify(m => m.PublicarComandoAsync(It.IsAny<string>(), It.IsAny<It.IsAnyType>()), Times.Once);

        Assert.Equal("queue.pix.processar", capturedQueue);
        Assert.NotNull(capturedMessage);

        var idProp = capturedMessage!.GetType().GetProperty("Id");
        var dadosProp = capturedMessage.GetType().GetProperty("Dados");

        Assert.NotNull(idProp);
        Assert.NotNull(dadosProp);

        var msgId = (Guid)idProp!.GetValue(capturedMessage)!;
        var msgDados = dadosProp!.GetValue(capturedMessage);

        Assert.Equal(protocolo, msgId);
        Assert.Same(request, msgDados);
    }
}
