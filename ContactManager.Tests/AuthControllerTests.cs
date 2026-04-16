using System.Security.Claims;
using ContactManager.API.Controllers;
using ContactManager.Application.DTOs.Auth;
using ContactManager.Domain.Entities;
using ContactManager.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ContactManager.Tests;

public class AuthControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
        
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            new Mock<IRoleStore<IdentityRole>>().Object,
            null!, null!, null!, null!);
        
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("test-jwt-key-32bytes-long-enough-for-test");

        _controller = new AuthController(_userManagerMock.Object, _roleManagerMock.Object, _configurationMock.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidMedico_ShouldReturnOk()
    {
        // Arrange
        var dto = new RegisterDTO
        {
            Email = "medico@teste.com",
            Password = "Medico@123",
            FullName = "Dr. João",
            Role = "Medico"
        };
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(r => r.RoleExistsAsync(UserRole.Medico.ToString()))
            .ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRole.Medico.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password), Times.Once);
        _userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRole.Medico.ToString()), Times.Once);
    }

    [Fact]
    public async Task Register_WithValidAdminAndCorrectCode_ShouldReturnOk()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ADMIN_REGISTRATION_CODE", "Admin@123");
        var dto = new RegisterDTO
        {
            Email = "admin@teste.com",
            Password = "Admin@123",
            FullName = "Admin",
            Role = "Admin",
            AdminCode = "Admin@123"
        };
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(r => r.RoleExistsAsync(UserRole.Admin.ToString()))
            .ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), UserRole.Admin.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Register_WithAdminButWrongCode_ShouldReturnBadRequest()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ADMIN_REGISTRATION_CODE", "Admin@123");
        var dto = new RegisterDTO
        {
            Email = "admin@teste.com",
            Password = "Admin@123",
            FullName = "Admin",
            Role = "Admin",
            AdminCode = "WrongCode"
        };

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Código de administrador inválido.");
        _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new RegisterDTO
        {
            Email = "invalid@teste.com",
            Password = "Test@123",
            FullName = "Invalid",
            Role = "Supervisor" // Role inválida
        };

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Role inválida. Valores permitidos: Medico, Admin");
    }

    [Fact]
    public async Task Register_WhenCreateFails_ShouldReturnBadRequestWithErrors()
    {
        // Arrange
        var dto = new RegisterDTO
        {
            Email = "fail@teste.com",
            Password = "Falha@123",
            FullName = "Falha",
            Role = "Medico"
        };
        var identityErrors = new[] { new IdentityError { Description = "Email já existe" } };
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var dto = new LoginDTO { Email = "medico@teste.com", Password = "Medico@123" };
        var user = new ApplicationUser { Id = "user-id", Email = dto.Email, UserName = dto.Email };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.CheckPasswordAsync(user, dto.Password))
            .ReturnsAsync(true);
        _userManagerMock.Setup(u => u.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Medico" });

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var tokenProperty = okResult!.Value!.GetType().GetProperty("token");
        tokenProperty.Should().NotBeNull();
        tokenProperty!.GetValue(okResult.Value).Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ShouldReturnUnauthorized()
    {
        // Arrange
        var dto = new LoginDTO { Email = "notfound@teste.com", Password = "AnyPass" };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var dto = new LoginDTO { Email = "medico@teste.com", Password = "WrongPass" };
        var user = new ApplicationUser { Id = "user-id", Email = dto.Email };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.CheckPasswordAsync(user, dto.Password))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);
    }

    #endregion

    #region ForgotPassword Tests

    [Fact]
    public async Task ForgotPassword_WithExistingEmail_ShouldReturnToken()
    {
        // Arrange
        var dto = new ForgotPasswordDTO { Email = "medico@teste.com" };
        var user = new ApplicationUser { Id = "user-id", Email = dto.Email };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token-123");

        // Act
        var result = await _controller.ForgotPassword(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var tokenProperty = okResult!.Value!.GetType().GetProperty("token");
        tokenProperty.Should().NotBeNull();
        tokenProperty!.GetValue(okResult.Value).Should().Be("reset-token-123");
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistingEmail_ShouldStillReturnOkForSecurity()
    {
        // Arrange
        var dto = new ForgotPasswordDTO { Email = "notfound@teste.com" };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.ForgotPassword(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        // Não revela se o email existe ou não
        _userManagerMock.Verify(u => u.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPassword_ForMedico_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var dto = new ResetPasswordDTO
        {
            Email = "medico@teste.com",
            Token = "valid-token",
            NewPassword = "NovaSenha@123"
        };
        var user = new ApplicationUser { Id = "user-id", Email = dto.Email };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsInRoleAsync(user, UserRole.Admin.ToString()))
            .ReturnsAsync(false); // não é admin
        _userManagerMock.Setup(u => u.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.ResetPassword(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ResetPassword_ForAdmin_WithValidTokenAndCorrectAdminCode_ShouldReturnOk()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ADMIN_REGISTRATION_CODE", "Admin@123");
        var dto = new ResetPasswordDTO
        {
            Email = "admin@teste.com",
            Token = "valid-token",
            NewPassword = "NovaAdmin@123",
            AdminCode = "Admin@123"
        };
        var user = new ApplicationUser { Id = "admin-id", Email = dto.Email };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsInRoleAsync(user, UserRole.Admin.ToString()))
            .ReturnsAsync(true);
        _userManagerMock.Setup(u => u.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.ResetPassword(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ResetPassword_ForAdmin_WithWrongAdminCode_ShouldReturnBadRequest()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ADMIN_REGISTRATION_CODE", "Admin@123");
        var dto = new ResetPasswordDTO
        {
            Email = "admin@teste.com",
            Token = "valid-token",
            NewPassword = "NovaAdmin@123",
            AdminCode = "WrongCode"
        };
        var user = new ApplicationUser { Id = "admin-id", Email = dto.Email };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsInRoleAsync(user, UserRole.Admin.ToString()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ResetPassword(dto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Código de administrador inválido para redefinir senha.");
        _userManagerMock.Verify(u => u.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_WithNonExistingUser_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new ResetPasswordDTO
        {
            Email = "notfound@teste.com",
            Token = "any",
            NewPassword = "any"
        };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.ResetPassword(dto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Usuário não encontrado.");
    }

    [Fact]
    public async Task ResetPassword_WhenResetFails_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new ResetPasswordDTO
        {
            Email = "medico@teste.com",
            Token = "invalid-token",
            NewPassword = "NovaSenha@123"
        };
        var user = new ApplicationUser { Id = "user-id", Email = dto.Email };
        var identityErrors = new[] { new IdentityError { Description = "Token inválido" } };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsInRoleAsync(user, UserRole.Admin.ToString()))
            .ReturnsAsync(false);
        _userManagerMock.Setup(u => u.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _controller.ResetPassword(dto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    #endregion
}
