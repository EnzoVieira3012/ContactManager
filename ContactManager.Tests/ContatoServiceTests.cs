using ContactManager.Application.DTOs;
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
            IsActive = true
        };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Contato>()))
            .ReturnsAsync(expectedContato);

        // Act
        var result = await _service.CreateAsync(dto);

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
        // Arrange
        var dto = new CreateContatoDTO
        {
            Nome = "Menor de Idade",
            DataNascimento = DateTime.Today.AddYears(-16),
            Sexo = "F"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contato>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithFutureDate_ShouldThrowArgumentException()
    {
        // Arrange
        var dto = new CreateContatoDTO
        {
            Nome = "Futuro",
            DataNascimento = DateTime.Today.AddDays(1),
            Sexo = "M"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contato>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithAgeZero_ShouldThrowArgumentException()
    {
        // Arrange
        var dto = new CreateContatoDTO
        {
            Nome = "Recém-nascido",
            DataNascimento = DateTime.Today,
            Sexo = "M"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contato>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingContact_ShouldUpdateAndReturnResponse()
    {
        // Arrange
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
            IsActive = true
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(existingContato);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contato>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be("João Atualizado");
        result.DataNascimento.Should().Be(dto.DataNascimento);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contato>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingContact_ShouldReturnNull()
    {
        // Arrange
        var dto = new UpdateContatoDTO { Id = 999, Nome = "Inexistente", DataNascimento = DateTime.Today.AddYears(-30), Sexo = "M" };
        _repositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Contato?)null);

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contato>()), Times.Never);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingActiveContact_ShouldReturnResponse()
    {
        // Arrange
        var contato = new Contato { Id = 1, Nome = "João", DataNascimento = new DateTime(1990, 1, 1), Sexo = "M", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(contato);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingContact_ShouldReturnNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Contato?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllActiveAsync Tests

    [Fact]
    public async Task GetAllActiveAsync_ShouldReturnOnlyActiveContacts()
    {
        // Arrange
        var active1 = new Contato { Id = 1, Nome = "Ativo1", IsActive = true };
        var active2 = new Contato { Id = 2, Nome = "Ativo2", IsActive = true };
        var inactive = new Contato { Id = 3, Nome = "Inativo", IsActive = false };
        var allActive = new List<Contato> { active1, active2 };
        _repositoryMock.Setup(r => r.GetAllActiveAsync())
            .ReturnsAsync(allActive);

        // Act
        var result = await _service.GetAllActiveAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().NotContain(c => !c.IsActive);
    }

    #endregion

    #region DeactivateAsync Tests

    [Fact]
    public async Task DeactivateAsync_WithExistingActiveContact_ShouldSetIsActiveFalse()
    {
        // Arrange
        var contato = new Contato { Id = 1, IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(contato);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contato>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeactivateAsync(1);

        // Assert
        result.Should().BeTrue();
        contato.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateAsync(contato), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WithNonExistingContact_ShouldReturnFalse()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Contato?)null);

        // Act
        var result = await _service.DeactivateAsync(999);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contato>()), Times.Never);
    }

    #endregion

    #region ActivateAsync Tests

    [Fact]
    public async Task ActivateAsync_WithExistingInactiveContact_ShouldSetIsActiveTrue()
    {
        // Arrange
        var contato = new Contato { Id = 1, IsActive = false };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(1))
            .ReturnsAsync(contato);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Contato>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ActivateAsync(1);

        // Assert
        result.Should().BeTrue();
        contato.IsActive.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateAsync(contato), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_WithAlreadyActiveContact_ShouldReturnTrueWithoutUpdate()
    {
        // Arrange
        var contato = new Contato { Id = 1, IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(1))
            .ReturnsAsync(contato);

        // Act
        var result = await _service.ActivateAsync(1);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contato>()), Times.Never);
    }

    [Fact]
    public async Task ActivateAsync_WithNonExistingContact_ShouldReturnFalse()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdIncludingInactiveAsync(999))
            .ReturnsAsync((Contato?)null);

        // Act
        var result = await _service.ActivateAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingActiveContact_ShouldRemove()
    {
        // Arrange
        var contato = new Contato { Id = 1 };
        _repositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(contato);
        _repositoryMock.Setup(r => r.DeleteAsync(1))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingContact_ShouldReturnFalse()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Contato?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion
}
