using MediatR;
using ServicoPix.Domain.Entities;
using ServicoPix.Domain.Events;
using ServicoPix.Domain.Interfaces;
using ServicoPix.Domain.Interfaces.IRepositories;
using ServicoPix.Domain.Interfaces.Services;

namespace ServicoPix.Application.UseCases.EfetivarPix.Commands;

public class EfetivarPixHandler : IRequestHandler<EfetivarPixCommand, bool>
{
    private readonly IContaRepository _contaRepo;
    private readonly ITransacaoRepository _transacaoRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMensageriaService _mensageria;

    public EfetivarPixHandler(IContaRepository contaRepo, ITransacaoRepository transacaoRepo, IUnitOfWork unitOfWork, IMensageriaService mensageria)
    {
        _contaRepo = contaRepo;
        _transacaoRepo = transacaoRepo;
        _unitOfWork = unitOfWork;
        _mensageria = mensageria;
    }

    public async Task<bool> Handle(EfetivarPixCommand request, CancellationToken cancellationToken)
    {
        var transacao = new Transacao(request.Id, request.ContaOrigemId, request.ContaDestinoId, request.Valor);
        await _transacaoRepo.AdicionarAsync(transacao);

        try
        {
            var origem = await _contaRepo.ObterPorIdAsync(request.ContaOrigemId);
            var destino = await _contaRepo.ObterPorIdAsync(request.ContaDestinoId);

            if (origem == null || destino == null) throw new Exception("Contas inválidas");

            origem.Debitar(request.Valor);
            destino.Creditar(request.Valor);

            _contaRepo.Atualizar(origem);
            _contaRepo.Atualizar(destino);

            transacao.ConcluirComSucesso();
            await _unitOfWork.CommitAsync(cancellationToken);

            await _mensageria.PublicarEventoAsync("topic.pix.fatos", new PixRealizadoEvent
            {
                TransacaoId = transacao.Id,
                Status = "SUCESSO"
            });
        }
        catch (Exception ex)
        {
            transacao.Falhar(ex.Message);
            await _unitOfWork.CommitAsync(cancellationToken);

            await _mensageria.PublicarEventoAsync("topic.pix.fatos", new PixRealizadoEvent
            {
                TransacaoId = transacao.Id,
                Status = "FALHA"
            });
            return false;
        }

        return true;
    }
}