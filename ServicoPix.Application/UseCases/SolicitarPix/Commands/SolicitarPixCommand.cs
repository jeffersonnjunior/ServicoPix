using MediatR;

namespace ServicoPix.Application.UseCases.SolicitarPix.Commands;

public class SolicitarPixCommand : IRequest<Guid>
{
    public Guid ContaOrigemId { get; set; }
    public Guid ContaDestinoId { get; set; }
    public decimal Valor { get; set; }
}