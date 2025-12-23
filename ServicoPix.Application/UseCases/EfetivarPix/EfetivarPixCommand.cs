using MediatR;

namespace ServicoPix.Application.UseCases.EfetivarPix.Commands;

public class EfetivarPixCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid ContaOrigemId { get; set; }
    public Guid ContaDestinoId { get; set; }
    public decimal Valor { get; set; }
}