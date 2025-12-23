using Microsoft.EntityFrameworkCore;
using ServicoPix.Domain.Entities;
using ServicoPix.Domain.Interfaces.IRepositories;
using ServicoPix.Infrastructure.Persistence.Context;

namespace ServicoPix.Infrastructure.Persistence.Repositories;

public class TransacaoRepository : ITransacaoRepository
{
    private readonly AppDbContext _context;

    public TransacaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(Transacao transacao)
    {
        await _context.Set<Transacao>().AddAsync(transacao);
    }

    public async Task<Transacao?> ObterPorIdAsync(Guid id)
    {
        return await _context.Set<Transacao>()
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}