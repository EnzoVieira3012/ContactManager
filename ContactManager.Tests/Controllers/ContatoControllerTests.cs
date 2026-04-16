using System.Security.Claims;
using ContactManager.API.Controllers;
using ContactManager.Application.DTOs.Contato;
using ContactManager.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ContactManager.Tests.Controllers;

public class ContatoControllerTests
{
    private readonly Mock<IContatoService> _serviceMock;
    private readonly ContatoController _controller;

    public ContatoControllerTests()
    {
        _serviceMock = new Mock<IContatoService>();
        _controller = new ContatoController(_serviceMock.Object);

        // Simula um usuário autenticado (role Medico por padrão)
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Role, "Medico")
        }, "mock"));
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateContatoDTO
        {
            Nome = "Teste",
            DataNascimento = DateTime.Today.AddYears(-20),
            Sexo = "M"
        };
        var response = new ContatoResponseDTO
        {
            Id = 1,
            Nome = "Teste",
            DataNascimento = dto.DataNascimento,
            Sexo = dto.Sexo,
            IsActive = true,
            Idade = 20
        };
        _serviceMock.Setup(s => s.CreateAsync(dto, "test-user-id"))
            .ReturnsAsync(response);

        var result = await _controller.Create(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(ContatoController.GetById));
        createdResult.RouteValues?["id"].Should().Be(1);
        createdResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task Create_WithInvalidAge_ReturnsBadRequest()
    {
        var dto = new CreateContatoDTO
        {
            Nome = "Menor",
            DataNascimento = DateTime.Today.AddYears(-16),
            Sexo = "F"
        };
        var errorMessage = "O contato deve ser maior de idade";
        _serviceMock.Setup(s => s.CreateAsync(dto, "test-user-id"))
            .ThrowsAsync(new ArgumentException(errorMessage));

        var result = await _controller.Create(dto);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var value = badRequestResult.Value;
        var errorProperty = value?.GetType().GetProperty("error");
        var actualError = errorProperty?.GetValue(value)?.ToString();
        actualError.Should().Be(errorMessage);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidDtoAndOwnership_ReturnsOk()
    {
        var dto = new UpdateContatoDTO
        {
            Id = 1,
            Nome = "Atualizado",
            DataNascimento = DateTime.Today.AddYears(-25),
            Sexo = "M"
        };
        var response = new ContatoResponseDTO { Id = 1, Nome = "Atualizado" };
        _serviceMock.Setup(s => s.UpdateAsync(dto, "test-user-id", false))
            .ReturnsAsync(response);

        var result = await _controller.Update(1, dto);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task Update_WithNonExistingContact_ReturnsNotFound()
    {
        var dto = new UpdateContatoDTO { Id = 999, Nome = "X", DataNascimento = DateTime.Today.AddYears(-30), Sexo = "M" };
        _serviceMock.Setup(s => s.UpdateAsync(dto, "test-user-id", false))
            .ReturnsAsync((ContatoResponseDTO?)null);

        var result = await _controller.Update(999, dto);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_WithUnauthorizedAccess_ReturnsForbid()
    {
        var dto = new UpdateContatoDTO { Id = 1, Nome = "Alheio", DataNascimento = DateTime.Today.AddYears(-30), Sexo = "M" };
        _serviceMock.Setup(s => s.UpdateAsync(dto, "test-user-id", false))
            .ThrowsAsync(new UnauthorizedAccessException("Sem permissão"));

        var result = await _controller.Update(1, dto);

        var forbidResult = result as ForbidResult;
        forbidResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_WithInvalidAge_ReturnsBadRequest()
    {
        var dto = new UpdateContatoDTO { Id = 1, Nome = "X", DataNascimento = DateTime.Today, Sexo = "M" };
        _serviceMock.Setup(s => s.UpdateAsync(dto, "test-user-id", false))
            .ThrowsAsync(new ArgumentException("Idade não pode ser 0"));

        var result = await _controller.Update(1, dto);

        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.StatusCode.Should().Be(400);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingContact_ReturnsOk()
    {
        var response = new ContatoResponseDTO { Id = 1, Nome = "João" };
        _serviceMock.Setup(s => s.GetByIdAsync(1, "test-user-id", false))
            .ReturnsAsync(response);

        var result = await _controller.GetById(1);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetById_WithNonExistingContact_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(999, "test-user-id", false))
            .ReturnsAsync((ContatoResponseDTO?)null);

        var result = await _controller.GetById(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_AsMedico_ReturnsOnlyUserContacts()
    {
        var contacts = new List<ContatoResponseDTO>
        {
            new() { Id = 1, Nome = "Meu contato" }
        };
        _serviceMock.Setup(s => s.GetAllActiveAsync("test-user-id", false))
            .ReturnsAsync(contacts);

        var result = await _controller.GetAll();

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(contacts);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsAllContacts()
    {
        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new(ClaimTypes.NameIdentifier, "admin-id"),
            new(ClaimTypes.Role, "Admin")
        }, "mock"));
        _controller.ControllerContext.HttpContext.User = adminUser;

        var allContacts = new List<ContatoResponseDTO>
        {
            new() { Id = 1, Nome = "Contato A" },
            new() { Id = 2, Nome = "Contato B" }
        };
        _serviceMock.Setup(s => s.GetAllActiveAsync("admin-id", true))
            .ReturnsAsync(allContacts);

        var result = await _controller.GetAll();

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(allContacts);
    }

    #endregion

    #region Desativar Tests

    [Fact]
    public async Task Desativar_WithExistingActiveContact_ReturnsNoContent()
    {
        _serviceMock.Setup(s => s.DeactivateAsync(1, "test-user-id", false))
            .ReturnsAsync(true);

        var result = await _controller.Desativar(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Desativar_WithNonExistingContact_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeactivateAsync(999, "test-user-id", false))
            .ReturnsAsync(false);

        var result = await _controller.Desativar(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Ativar Tests

    [Fact]
    public async Task Ativar_WithExistingInactiveContact_ReturnsNoContent()
    {
        _serviceMock.Setup(s => s.ActivateAsync(1, "test-user-id", false))
            .ReturnsAsync(true);

        var result = await _controller.Ativar(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Ativar_WithNonExistingContact_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.ActivateAsync(999, "test-user-id", false))
            .ReturnsAsync(false);

        var result = await _controller.Ativar(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingContact_ReturnsNoContent()
    {
        _serviceMock.Setup(s => s.DeleteAsync(1, "test-user-id", false))
            .ReturnsAsync(true);

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistingContact_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteAsync(999, "test-user-id", false))
            .ReturnsAsync(false);

        var result = await _controller.Delete(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion
}
