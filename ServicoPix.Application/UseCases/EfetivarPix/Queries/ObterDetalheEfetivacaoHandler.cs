using MediatR;
using ServicoPix.Domain.Interfaces.IRepositories;

namespace ServicoPix.Application.UseCases.EfetivarPix.Queries;

public class ObterDetalheEfetivacaoHandler : IRequestHandler<ObterDetalheEfetivacaoQuery, DetalheEfetivacaoDto?>
{
    private readonly ITransacaoRepository _repo;

    public ObterDetalheEfetivacaoHandler(ITransacaoRepository repo)
    {
        _repo = repo;
    }

    public async Task<DetalheEfetivacaoDto?> Handle(ObterDetalheEfetivacaoQuery request, CancellationToken cancellationToken)
    {
        var transacao = await _repo.ObterPorIdAsync(request.TransacaoId);

        if (transacao == null) return null;

        return new DetalheEfetivacaoDto
        {
            TransacaoId = transacao.Id,
            Status = transacao.Status.ToString(),
            DataProcessamento = transacao.DataCriacao,
            MotivoFalha = transacao.MensagemErro
        };
    }
}