using ContactManager.Domain.Entities;

namespace ContactManager.Domain.Interfaces;

public interface IContatoRepository
{
    Task<Contato?> GetByIdAsync(int id);
    Task<IEnumerable<Contato>> GetAllActiveAsync();
    Task<IEnumerable<Contato>> GetAllActiveByUserAsync(string userId);
    Task<Contato> AddAsync(Contato contato);
    Task UpdateAsync(Contato contato);
    Task DeleteAsync(int id);
    Task<bool> ExistsActiveAsync(int id);
    Task<Contato?> GetByIdIncludingInactiveAsync(int id);
}
