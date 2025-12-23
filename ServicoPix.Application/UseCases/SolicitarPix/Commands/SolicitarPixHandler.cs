using MediatR;
using ServicoPix.Domain.Interfaces.Services;

namespace ServicoPix.Application.UseCases.SolicitarPix.Commands;

public class SolicitarPixHandler : IRequestHandler<SolicitarPixCommand, Guid>
{
    private readonly IMensageriaService _mensageria;

    public SolicitarPixHandler(IMensageriaService mensageria)
    {
        _mensageria = mensageria;
    }

    public async Task<Guid> Handle(SolicitarPixCommand request, CancellationToken cancellationToken)
    {
        var protocolo = Guid.NewGuid();

        var mensagem = new { Id = protocolo, Dados = request };

        await _mensageria.PublicarComandoAsync("queue.pix.processar", mensagem);

        return protocolo;
    }
}