using Moq;
using ServicoPix.Application.UseCases.EfetivarPix.Queries;
using ServicoPix.Application.UseCases.SolicitarPix.Queries;
using ServicoPix.Domain.Entities;
using ServicoPix.Domain.Enums;
using ServicoPix.Domain.Interfaces.IRepositories;
using Xunit;

namespace ServicoPix.Tests.UseCases;

public class QueryHandlersTests
{
    [Fact]
    public async Task ObterPixPorId_returns_null_when_not_found()
    {
        var repo = new Mock<ITransacaoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Transacao?)null);

        var handler = new ObterPixPorIdHandler(repo.Object);

        var result = await handler.Handle(new ObterPixPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
        repo.VerifyAll();
    }

    [Fact]
    public async Task ObterPixPorId_maps_transacao_to_dto()
    {
        var transacao = new Transacao(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 55m);
        transacao.ConcluirComSucesso();

        var repo = new Mock<ITransacaoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.ObterPorIdAsync(transacao.Id)).ReturnsAsync(transacao);

        var handler = new ObterPixPorIdHandler(repo.Object);

        var dto = await handler.Handle(new ObterPixPorIdQuery(transacao.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(transacao.Id, dto!.Id);
        Assert.Equal(55m, dto.Valor);
        Assert.Equal(StatusTransacao.Concluido.ToString(), dto.Status);
        repo.VerifyAll();
    }

    [Fact]
    public async Task ObterDetalheEfetivacao_returns_null_when_not_found()
    {
        var repo = new Mock<ITransacaoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Transacao?)null);

        var handler = new ObterDetalheEfetivacaoHandler(repo.Object);

        var result = await handler.Handle(new ObterDetalheEfetivacaoQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
        repo.VerifyAll();
    }

    [Fact]
    public async Task ObterDetalheEfetivacao_maps_transacao_to_dto_including_error_message()
    {
        var transacao = new Transacao(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m);
        transacao.Falhar("Saldo insuficiente");

        var repo = new Mock<ITransacaoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.ObterPorIdAsync(transacao.Id)).ReturnsAsync(transacao);

        var handler = new ObterDetalheEfetivacaoHandler(repo.Object);

        var dto = await handler.Handle(new ObterDetalheEfetivacaoQuery(transacao.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(transacao.Id, dto!.TransacaoId);
        Assert.Equal(StatusTransacao.Falha.ToString(), dto.Status);
        Assert.Equal(transacao.DataCriacao, dto.DataProcessamento);
        Assert.Equal("Saldo insuficiente", dto.MotivoFalha);
        repo.VerifyAll();
    }
}
