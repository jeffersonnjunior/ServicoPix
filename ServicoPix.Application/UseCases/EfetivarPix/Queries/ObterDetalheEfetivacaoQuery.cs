using MediatR;

namespace ServicoPix.Application.UseCases.EfetivarPix.Queries;

public class ObterDetalheEfetivacaoQuery : IRequest<DetalheEfetivacaoDto?>
{
    public Guid TransacaoId { get; set; }

    public ObterDetalheEfetivacaoQuery(Guid transacaoId)
    {
        TransacaoId = transacaoId;
    }
}