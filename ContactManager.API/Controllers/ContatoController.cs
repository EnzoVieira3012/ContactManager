using System.Security.Claims;
using ContactManager.Application.DTOs.Contato;
using ContactManager.Application.Interfaces;
using ContactManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContatoController : ControllerBase
{
    private readonly IContatoService _contatoService;

    public ContatoController(IContatoService contatoService)
    {
        _contatoService = contatoService;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Usuário não identificado");

    private bool IsAdmin() => User.IsInRole(UserRole.Admin.ToString());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContatoDTO dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _contatoService.CreateAsync(dto, userId);
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
            var userId = GetUserId();
            var isAdmin = IsAdmin();
            var result = await _contatoService.UpdateAsync(dto, userId, isAdmin);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var isAdmin = IsAdmin();
        var contato = await _contatoService.GetByIdAsync(id, userId, isAdmin);
        if (contato == null) return NotFound();
        return Ok(contato);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var isAdmin = IsAdmin();
        var contatos = await _contatoService.GetAllActiveAsync(userId, isAdmin);
        return Ok(contatos);
    }

    [HttpPatch("{id}/desativar")]
    public async Task<IActionResult> Desativar(int id)
    {
        var userId = GetUserId();
        var isAdmin = IsAdmin();
        var success = await _contatoService.DeactivateAsync(id, userId, isAdmin);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPatch("{id}/ativar")]
    public async Task<IActionResult> Ativar(int id)
    {
        var userId = GetUserId();
        var isAdmin = IsAdmin();
        var success = await _contatoService.ActivateAsync(id, userId, isAdmin);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var isAdmin = IsAdmin();
        var success = await _contatoService.DeleteAsync(id, userId, isAdmin);
        if (!success) return NotFound();
        return NoContent();
    }
}
