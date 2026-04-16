using ContactManager.Application.DTOs.Contato;

namespace ContactManager.Application.Interfaces;

public interface IContatoService
{
    Task<ContatoResponseDTO> CreateAsync(CreateContatoDTO dto, string userId);
    Task<ContatoResponseDTO?> UpdateAsync(UpdateContatoDTO dto, string userId, bool isAdmin);
    Task<ContatoResponseDTO?> GetByIdAsync(int id, string userId, bool isAdmin);
    Task<IEnumerable<ContatoResponseDTO>> GetAllActiveAsync(string userId, bool isAdmin);
    Task<bool> DeactivateAsync(int id, string userId, bool isAdmin);
    Task<bool> ActivateAsync(int id, string userId, bool isAdmin);
    Task<bool> DeleteAsync(int id, string userId, bool isAdmin);
}
