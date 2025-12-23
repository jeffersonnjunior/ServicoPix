using ServicoPix.Domain.Entities;

namespace ServicoPix.Domain.Interfaces.IRepositories;

public interface IContaRepository
{
    Task<Conta?> ObterPorIdAsync(Guid id);
    void Atualizar(Conta conta);
}