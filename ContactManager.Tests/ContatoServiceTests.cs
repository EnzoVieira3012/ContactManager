using ContactManager.Application.DTOs.Contato;
using ContactManager.Application.Services;
using ContactManager.Application.Validators;
using ContactManager.Domain.Entities;
using ContactManager.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ContactManager.Tests;

public class ContatoServiceTests
{
    private readonly Mock<IContatoRepository> _repositoryMock;
    private readonly ContatoService _service;
    private readonly string _testUserId = "test-user-id";
    private readonly bool _isAdmin = false;

    public ContatoServiceTests()
    {
        _repositoryMock = new Mock<IContatoRepository>();
        _service = new ContatoService(_repositoryMock.Object);
    }

    #region Helper method for age calculation
    private static int CalcularIdade(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateAndReturnResponse()
    {
        // Arrange
        var dto = new CreateContatoDTO
        {
            Nome = "João Silva",
            DataNascimento = new DateTime(1990, 5, 10),
            Sexo = "M"
        };
        var expectedContato = new Contato
        {
            Id = 1,
            Nome = dto.Nome,
            DataNascimento = dto.DataNascimento,
            Sexo = dto.Sexo,
            IsActive = true,
            UserId = _testUserId
        };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Contato>()))
            .ReturnsAsync(expectedContato);

        // Act
        var result = await _service.CreateAsync(dto, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Nome.Should().Be("João Silva");
        
        var idadeEsperada = CalcularIdade(dto.DataNascimento);
        result.Idade.Should().Be(idadeEsperada);
        
        result.IsActive.Should().BeTrue();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contato>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithUnderAge_ShouldThrowArgumentException()
    {
        var dto = new CreateContatoDTO
        {
            Nome = "Menor de Idade",
            DataNascimento = DateTime.Today.AddYears(-16),
            Sexo = "F"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto, _testUserId));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contato>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithFutureDate_ShouldThrowArgumentException()
    {
        var dto = new CreateContatoDTO
        {
            Nome = "Futuro",
            DataNascimento = DateTime.Today.AddDays(1),
            Sexo = "M"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto, _testUserId));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contato>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithAgeZero_ShouldThrowArgumentException()
    {
        var dto = new CreateContatoDTO
        {
            Nome = "Recém-nascido",
            DataNascimento = DateTime.Today,
            Sexo = "M"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto, _testUserId));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contato>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingContactOwnedByUser_ShouldUpdateAndReturnResponse()
    {
        var dto = new UpdateContatoDTO
        {
            Id = 1,
            Nome = "João Atualizado",
            DataNascimento = new DateTime(1985, 3, 20),
            Sexo = "M"
        };
        var existingContato = new Contato
        {
            Id = 1,
            Nome = "João Silva",
            DataNascimento = new DateTime(1990, 5, 10),
            Sexo = "M",
            IsActive = true,
            UserId = _testUserId
        };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(1))
            .ReturnsAsync(existingContato);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contato>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(dto, _testUserId, _isAdmin);

        result.Should().NotBeNull();
        result.Nome.Should().Be("João Atualizado");
        result.DataNascimento.Should().Be(dto.DataNascimento);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contato>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingContact_ShouldReturnNull()
    {
        var dto = new UpdateContatoDTO { Id = 999, Nome = "Inexistente", DataNascimento = DateTime.Today.AddYears(-30), Sexo = "M" };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(999))
            .ReturnsAsync((Contato?)null);

        var result = await _service.UpdateAsync(dto, _testUserId, _isAdmin);

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contato>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithContactFromDifferentUser_ShouldThrowUnauthorized()
    {
        var dto = new UpdateContatoDTO { Id = 1, Nome = "Alheio", DataNascimento = DateTime.Today.AddYears(-30), Sexo = "M" };
        var existingContato = new Contato { Id = 1, UserId = "other-user", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(1))
            .ReturnsAsync(existingContato);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _service.UpdateAsync(dto, _testUserId, false));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingActiveContactOwnedByUser_ShouldReturnResponse()
    {
        var contato = new Contato { Id = 1, Nome = "João", DataNascimento = new DateTime(1990, 1, 1), Sexo = "M", IsActive = true, UserId = _testUserId };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(1))
            .ReturnsAsync(contato);

        var result = await _service.GetByIdAsync(1, _testUserId, false);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingContact_ShouldReturnNull()
    {
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(999))
            .ReturnsAsync((Contato?)null);

        var result = await _service.GetByIdAsync(999, _testUserId, false);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithContactFromDifferentUser_ShouldReturnNull()
    {
        var contato = new Contato { Id = 1, UserId = "other-user" };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(1))
            .ReturnsAsync(contato);

        var result = await _service.GetByIdAsync(1, _testUserId, false);
        result.Should().BeNull();
    }

    #endregion

    #region GetAllActiveAsync Tests

    [Fact]
    public async Task GetAllActiveAsync_AsNonAdmin_ShouldReturnOnlyUserContacts()
    {
        var userContacts = new List<Contato> 
        { 
            new Contato { Id = 1, IsActive = true, UserId = _testUserId },
            new Contato { Id = 2, IsActive = true, UserId = _testUserId }
        };
        _repositoryMock.Setup(r => r.GetAllActiveByUserAsync(_testUserId))
            .ReturnsAsync(userContacts);

        var result = await _service.GetAllActiveAsync(_testUserId, false);

        result.Should().HaveCount(2);
        _repositoryMock.Verify(r => r.GetAllActiveByUserAsync(_testUserId), Times.Once);
        _repositoryMock.Verify(r => r.GetAllActiveAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAllActiveAsync_AsAdmin_ShouldReturnAllActiveContacts()
    {
        var allActive = new List<Contato>
        {
            new Contato { Id = 1, IsActive = true, UserId = "user1" },
            new Contato { Id = 2, IsActive = true, UserId = "user2" }
        };
        _repositoryMock.Setup(r => r.GetAllActiveAsync())
            .ReturnsAsync(allActive);

        var result = await _service.GetAllActiveAsync(_testUserId, true);

        result.Should().HaveCount(2);
        _repositoryMock.Verify(r => r.GetAllActiveAsync(), Times.Once);
        _repositoryMock.Verify(r => r.GetAllActiveByUserAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region DeactivateAsync Tests

    [Fact]
    public async Task DeactivateAsync_WithExistingActiveContactOwnedByUser_ShouldSetIsActiveFalse()
    {
        var contato = new Contato { Id = 1, IsActive = true, UserId = _testUserId };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(1))
            .ReturnsAsync(contato);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contato>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeactivateAsync(1, _testUserId, false);

        result.Should().BeTrue();
        contato.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateAsync(contato), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WithNonExistingContact_ShouldReturnFalse()
    {
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(999))
            .ReturnsAsync((Contato?)null);

        var result = await _service.DeactivateAsync(999, _testUserId, false);
        result.Should().BeFalse();
    }

    #endregion

    #region ActivateAsync Tests

    [Fact]
    public async Task ActivateAsync_WithExistingInactiveContactOwnedByUser_ShouldSetIsActiveTrue()
    {
        var contato = new Contato { Id = 1, IsActive = false, UserId = _testUserId };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(1))
            .ReturnsAsync(contato);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contato>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ActivateAsync(1, _testUserId, false);

        result.Should().BeTrue();
        contato.IsActive.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateAsync(contato), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_WithAlreadyActiveContact_ShouldReturnTrueWithoutUpdate()
    {
        var contato = new Contato { Id = 1, IsActive = true, UserId = _testUserId };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(1))
            .ReturnsAsync(contato);

        var result = await _service.ActivateAsync(1, _testUserId, false);
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contato>()), Times.Never);
    }

    [Fact]
    public async Task ActivateAsync_WithNonExistingContact_ShouldReturnFalse()
    {
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(999))
            .ReturnsAsync((Contato?)null);

        var result = await _service.ActivateAsync(999, _testUserId, false);
        result.Should().BeFalse();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingContactOwnedByUser_ShouldRemove()
    {
        var contato = new Contato { Id = 1, UserId = _testUserId };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(1))
            .ReturnsAsync(contato);
        _repositoryMock.Setup(r => r.DeleteAsync(1))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1, _testUserId, false);
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingContact_ShouldReturnFalse()
    {
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(999))
            .ReturnsAsync((Contato?)null);

        var result = await _service.DeleteAsync(999, _testUserId, false);
        result.Should().BeFalse();
    }

    #endregion
}
