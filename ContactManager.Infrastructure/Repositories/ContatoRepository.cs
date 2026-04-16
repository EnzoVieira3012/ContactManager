using ContactManager.Domain.Entities;
using ContactManager.Domain.Interfaces;
using ContactManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContactManager.Infrastructure.Repositories;

public class ContatoRepository : IContatoRepository
{
    private readonly AppDbContext _context;

    public ContatoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Contato?> GetByIdAsync(int id)
    {
        return await _context.Contatos
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
    }

    public async Task<IEnumerable<Contato>> GetAllActiveAsync()
    {
        return await _context.Contatos
            .Where(c => c.IsActive)
            .ToListAsync();
    }

    public async Task<Contato> AddAsync(Contato contato)
    {
        _context.Contatos.Add(contato);
        await _context.SaveChangesAsync();
        return contato;
    }

    public async Task UpdateAsync(Contato contato)
    {
        _context.Entry(contato).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var contato = await _context.Contatos.FindAsync(id);
        if (contato != null)
        {
            _context.Contatos.Remove(contato);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsActiveAsync(int id)
    {
        return await _context.Contatos.AnyAsync(c => c.Id == id && c.IsActive);
    }

    public async Task<Contato?> GetByIdIncludingInactiveAsync(int id)
    {
        return await _context.Contatos.FindAsync(id);
    }
}
