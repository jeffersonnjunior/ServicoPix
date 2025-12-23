using MediatR;
using ServicoPix.Domain.Interfaces.IRepositories;

namespace ServicoPix.Application.UseCases.SolicitarPix.Queries;

public class ObterPixPorIdHandler : IRequestHandler<ObterPixPorIdQuery, PixDto?>
{
    private readonly ITransacaoRepository _repo;

    public ObterPixPorIdHandler(ITransacaoRepository repo)
    {
        _repo = repo;
    }

    public async Task<PixDto?> Handle(ObterPixPorIdQuery request, CancellationToken cancellationToken)
    {
        var t = await _repo.ObterPorIdAsync(request.Id);
        if (t == null) return null;

        return new PixDto
        {
            Id = t.Id,
            Valor = t.Valor,
            Status = t.Status.ToString()
        };
    }
}