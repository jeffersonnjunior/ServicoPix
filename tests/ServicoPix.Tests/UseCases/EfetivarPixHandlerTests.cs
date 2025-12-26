using Moq;
using ServicoPix.Application.UseCases.EfetivarPix.Commands;
using ServicoPix.Domain.Entities;
using ServicoPix.Domain.Enums;
using ServicoPix.Domain.Events;
using ServicoPix.Domain.Interfaces;
using ServicoPix.Domain.Interfaces.IRepositories;
using ServicoPix.Domain.Interfaces.Services;
using Xunit;

namespace ServicoPix.Tests.UseCases;

public class EfetivarPixHandlerTests
{
    [Fact]
    public async Task Handle_success_debits_and_credits_accounts_commits_and_publishes_success_event()
    {
        var origem = new Conta(Guid.NewGuid(), "0001", 100m);
        var destino = new Conta(Guid.NewGuid(), "0002", 0m);

        var contaRepo = new Mock<IContaRepository>(MockBehavior.Strict);
        var transacaoRepo = new Mock<ITransacaoRepository>(MockBehavior.Strict);
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var mensageria = new Mock<IMensageriaService>(MockBehavior.Strict);

        Transacao? capturedTransacao = null;
        PixRealizadoEvent? capturedEvent = null;
        string? capturedTopic = null;

        transacaoRepo
            .Setup(r => r.AdicionarAsync(It.IsAny<Transacao>()))
            .Callback<Transacao>(t => capturedTransacao = t)
            .Returns(Task.CompletedTask);

        contaRepo
            .Setup(r => r.ObterPorIdAsync(origem.Id))
            .ReturnsAsync(origem);
        contaRepo
            .Setup(r => r.ObterPorIdAsync(destino.Id))
            .ReturnsAsync(destino);

        contaRepo.Setup(r => r.Atualizar(origem));
        contaRepo.Setup(r => r.Atualizar(destino));

        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        mensageria
            .Setup(m => m.PublicarEventoAsync("topic.pix.fatos", It.IsAny<PixRealizadoEvent>()))
            .Callback<string, PixRealizadoEvent>((topic, evt) =>
            {
                capturedTopic = topic;
                capturedEvent = evt;
            })
            .Returns(Task.CompletedTask);

        var handler = new EfetivarPixHandler(contaRepo.Object, transacaoRepo.Object, uow.Object, mensageria.Object);

        var cmd = new EfetivarPixCommand
        {
            Id = Guid.NewGuid(),
            ContaOrigemId = origem.Id,
            ContaDestinoId = destino.Id,
            Valor = 30m
        };

        var ok = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(ok);

        Assert.Equal(70m, origem.Saldo);
        Assert.Equal(30m, destino.Saldo);

        Assert.NotNull(capturedTransacao);
        Assert.Equal(StatusTransacao.Concluido, capturedTransacao!.Status);

        Assert.Equal("topic.pix.fatos", capturedTopic);
        Assert.NotNull(capturedEvent);
        Assert.Equal(capturedTransacao.Id, capturedEvent!.TransacaoId);
        Assert.Equal("SUCESSO", capturedEvent.Status);

        transacaoRepo.VerifyAll();
        contaRepo.VerifyAll();
        uow.VerifyAll();
        mensageria.VerifyAll();
    }

    [Fact]
    public async Task Handle_failure_marks_transaction_as_failed_commits_and_publishes_failure_event()
    {
        var origem = new Conta(Guid.NewGuid(), "0001", 10m);
        var destino = new Conta(Guid.NewGuid(), "0002", 0m);

        var contaRepo = new Mock<IContaRepository>(MockBehavior.Strict);
        var transacaoRepo = new Mock<ITransacaoRepository>(MockBehavior.Strict);
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var mensageria = new Mock<IMensageriaService>(MockBehavior.Strict);

        Transacao? capturedTransacao = null;
        PixRealizadoEvent? capturedEvent = null;

        transacaoRepo
            .Setup(r => r.AdicionarAsync(It.IsAny<Transacao>()))
            .Callback<Transacao>(t => capturedTransacao = t)
            .Returns(Task.CompletedTask);

        contaRepo
            .Setup(r => r.ObterPorIdAsync(origem.Id))
            .ReturnsAsync(origem);
        contaRepo
            .Setup(r => r.ObterPorIdAsync(destino.Id))
            .ReturnsAsync(destino);

        // No Atualizar expected on failure due to insufficient funds.

        uow.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        mensageria
            .Setup(m => m.PublicarEventoAsync("topic.pix.fatos", It.IsAny<PixRealizadoEvent>()))
            .Callback<string, PixRealizadoEvent>((_, evt) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        var handler = new EfetivarPixHandler(contaRepo.Object, transacaoRepo.Object, uow.Object, mensageria.Object);

        var cmd = new EfetivarPixCommand
        {
            Id = Guid.NewGuid(),
            ContaOrigemId = origem.Id,
            ContaDestinoId = destino.Id,
            Valor = 30m
        };

        var ok = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(ok);

        Assert.NotNull(capturedTransacao);
        Assert.Equal(StatusTransacao.Falha, capturedTransacao!.Status);
        Assert.False(string.IsNullOrWhiteSpace(capturedTransacao.MensagemErro));

        Assert.NotNull(capturedEvent);
        Assert.Equal(capturedTransacao.Id, capturedEvent!.TransacaoId);
        Assert.Equal("FALHA", capturedEvent.Status);

        transacaoRepo.VerifyAll();
        contaRepo.VerifyAll();
        uow.VerifyAll();
        mensageria.VerifyAll();
    }
}
