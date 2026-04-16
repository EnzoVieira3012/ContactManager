using ContactManager.Domain.Entities;
using ContactManager.Domain.Interfaces;
using ContactManager.Application.Validators;
using ContactManager.Application.Interfaces;
using ContactManager.Application.DTOs.Contato;

namespace ContactManager.Application.Services;

public class ContatoService : IContatoService
{
    private readonly IContatoRepository _repository;

    public ContatoService(IContatoRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContatoResponseDTO> CreateAsync(CreateContatoDTO dto, string userId)
    {
        ContatoValidator.Validate(dto.DataNascimento);

        var contato = new Contato
        {
            Nome = dto.Nome,
            DataNascimento = dto.DataNascimento,
            Sexo = dto.Sexo,
            IsActive = true,
            UserId = userId
        };

        var created = await _repository.AddAsync(contato);
        return MapToResponse(created);
    }

    public async Task<ContatoResponseDTO?> UpdateAsync(UpdateContatoDTO dto, string userId, bool isAdmin)
    {
        var contato = await _repository.GetByIdIncludingInactiveAsync(dto.Id);
        if (contato == null) return null;

        if (!isAdmin && contato.UserId != userId)
            throw new UnauthorizedAccessException("Você não tem permissão para alterar este contato.");

        ContatoValidator.Validate(dto.DataNascimento);

        contato.Nome = dto.Nome;
        contato.DataNascimento = dto.DataNascimento;
        contato.Sexo = dto.Sexo;

        await _repository.UpdateAsync(contato);
        return MapToResponse(contato);
    }

    public async Task<ContatoResponseDTO?> GetByIdAsync(int id, string userId, bool isAdmin)
    {
        var contato = await _repository.GetByIdIncludingInactiveAsync(id);
        if (contato == null) return null;
        if (!isAdmin && contato.UserId != userId) return null;
        return MapToResponse(contato);
    }

    public async Task<IEnumerable<ContatoResponseDTO>> GetAllActiveAsync(string userId, bool isAdmin)
    {
        IEnumerable<Contato> contatos;
        if (isAdmin)
            contatos = await _repository.GetAllActiveAsync();      // busca todos ativos
        else
            contatos = await _repository.GetAllActiveByUserAsync(userId); // só os do médico

        return contatos.Select(MapToResponse);
    }

    public async Task<bool> DeactivateAsync(int id, string userId, bool isAdmin)
    {
        var contato = await _repository.GetByIdIncludingInactiveAsync(id);
        if (contato == null) return false;
        if (!isAdmin && contato.UserId != userId) return false;

        contato.IsActive = false;
        await _repository.UpdateAsync(contato);
        return true;
    }

    public async Task<bool> ActivateAsync(int id, string userId, bool isAdmin)
    {
        var contato = await _repository.GetByIdIncludingInactiveAsync(id);
        if (contato == null) return false;
        if (!isAdmin && contato.UserId != userId) return false;

        contato.IsActive = true;
        await _repository.UpdateAsync(contato);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, string userId, bool isAdmin)
    {
        var contato = await _repository.GetByIdIncludingInactiveAsync(id);
        if (contato == null) return false;
        if (!isAdmin && contato.UserId != userId) return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    private ContatoResponseDTO MapToResponse(Contato c)
    {
        return new ContatoResponseDTO
        {
            Id = c.Id,
            Nome = c.Nome,
            DataNascimento = c.DataNascimento,
            Sexo = c.Sexo,
            Idade = c.Idade,
            IsActive = c.IsActive
        };
    }
}
