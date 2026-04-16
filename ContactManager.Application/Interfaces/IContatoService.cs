using ContactManager.Application.DTOs;

namespace ContactManager.Application.Interfaces;

public interface IContatoService
{
    Task<ContatoResponseDTO> CreateAsync(CreateContatoDTO dto);
    Task<ContatoResponseDTO?> UpdateAsync(UpdateContatoDTO dto);
    Task<ContatoResponseDTO?> GetByIdAsync(int id);
    Task<IEnumerable<ContatoResponseDTO>> GetAllActiveAsync();
    Task<bool> DeactivateAsync(int id);
    Task<bool> ActivateAsync(int id);
    Task<bool> DeleteAsync(int id);
}
