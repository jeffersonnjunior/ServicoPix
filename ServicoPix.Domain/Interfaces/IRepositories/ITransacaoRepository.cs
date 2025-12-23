using ServicoPix.Domain.Entities;

namespace ServicoPix.Domain.Interfaces.IRepositories;

public interface ITransacaoRepository
{
    Task AdicionarAsync(Transacao transacao);
    Task<Transacao?> ObterPorIdAsync(Guid id);
}