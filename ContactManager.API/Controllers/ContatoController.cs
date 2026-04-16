using ContactManager.Application.DTOs;
using ContactManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContatoController : ControllerBase
{
    private readonly IContatoService _contatoService;

    public ContatoController(IContatoService contatoService)
    {
        _contatoService = contatoService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContatoDTO dto)
    {
        try
        {
            var result = await _contatoService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContatoDTO dto)
    {
        if (id != dto.Id) return BadRequest("Id da URL não confere com o corpo.");
        try
        {
            var result = await _contatoService.UpdateAsync(dto);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contato = await _contatoService.GetByIdAsync(id);
        if (contato == null) return NotFound();
        return Ok(contato);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var contatos = await _contatoService.GetAllActiveAsync();
        return Ok(contatos);
    }

    [HttpPatch("{id}/desativar")]
    public async Task<IActionResult> Desativar(int id)
    {
        var success = await _contatoService.DeactivateAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPatch("{id}/ativar")]
    public async Task<IActionResult> Ativar(int id)
    {
        var success = await _contatoService.ActivateAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _contatoService.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
