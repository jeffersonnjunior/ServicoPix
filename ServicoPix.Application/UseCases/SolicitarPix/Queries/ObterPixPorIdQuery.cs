using MediatR;

namespace ServicoPix.Application.UseCases.SolicitarPix.Queries;

public class ObterPixPorIdQuery : IRequest<PixDto?>
{
    public Guid Id { get; set; }
    public ObterPixPorIdQuery(Guid id) => Id = id;
}
