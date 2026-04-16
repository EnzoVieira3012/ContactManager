using ContactManager.Domain.Entities;
using ContactManager.Domain.Interfaces;
using ContactManager.Application.DTOs;
using ContactManager.Application.Validators;
using ContactManager.Application.Interfaces;

namespace ContactManager.Application.Services;

public class ContatoService : IContatoService
{
    private readonly IContatoRepository _repository;

    public ContatoService(IContatoRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContatoResponseDTO> CreateAsync(CreateContatoDTO dto)
    {
        ContatoValidator.Validate(dto.DataNascimento);

        var contato = new Contato
        {
            Nome = dto.Nome,
            DataNascimento = dto.DataNascimento,
            Sexo = dto.Sexo,
            IsActive = true
        };

        var created = await _repository.AddAsync(contato);
        return MapToResponse(created);
    }

    public async Task<ContatoResponseDTO?> UpdateAsync(UpdateContatoDTO dto)
    {
        var contato = await _repository.GetByIdAsync(dto.Id);
        if (contato == null) return null;

        ContatoValidator.Validate(dto.DataNascimento);

        contato.Nome = dto.Nome;
        contato.DataNascimento = dto.DataNascimento;
        contato.Sexo = dto.Sexo;

        await _repository.UpdateAsync(contato);
        return MapToResponse(contato);
    }

    public async Task<ContatoResponseDTO?> GetByIdAsync(int id)
    {
        var contato = await _repository.GetByIdAsync(id);
        return contato == null ? null : MapToResponse(contato);
    }

    public async Task<IEnumerable<ContatoResponseDTO>> GetAllActiveAsync()
    {
        var contatos = await _repository.GetAllActiveAsync();
        return contatos.Select(MapToResponse);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var contato = await _repository.GetByIdAsync(id);
        if (contato == null) return false;

        contato.IsActive = false;
        await _repository.UpdateAsync(contato);
        return true;
    }

    public async Task<bool> ActivateAsync(int id)
    {
        var contato = await _repository.GetByIdIncludingInactiveAsync(id);
        if (contato == null) return false;
        if (contato.IsActive) return true;

        contato.IsActive = true;
        await _repository.UpdateAsync(contato);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var contato = await _repository.GetByIdAsync(id);
        if (contato == null) return false;

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
