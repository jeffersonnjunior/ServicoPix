using Microsoft.EntityFrameworkCore;
using ServicoPix.Domain.Entities;
using ServicoPix.Domain.Interfaces.IRepositories;
using ServicoPix.Infrastructure.Persistence.Context;

namespace ServicoPix.Infrastructure.Persistence.Repositories;

public class ContaRepository : IContaRepository
{
    private readonly AppDbContext _context;

    public ContaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Conta?> ObterPorIdAsync(Guid id)
    {
        return await _context.Set<Conta>()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public void Atualizar(Conta conta)
    {
        _context.Set<Conta>().Update(conta);
    }
}
