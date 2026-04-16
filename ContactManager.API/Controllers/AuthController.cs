using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ContactManager.Application.DTOs.Auth;
using ContactManager.Domain.Entities;
using ContactManager.Domain.Enums;

namespace ContactManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<ApplicationUser> userManager,
                          RoleManager<IdentityRole> roleManager,
                          IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDTO model)
    {
        if (!Enum.TryParse<UserRole>(model.Role, true, out var role))
            return BadRequest("Role inválida. Valores permitidos: Medico, Admin");

        if (role == UserRole.Admin)
        {
            var expectedCode = Environment.GetEnvironmentVariable("ADMIN_REGISTRATION_CODE") ?? "Admin@123";
            if (string.IsNullOrEmpty(model.AdminCode) || model.AdminCode != expectedCode)
                return BadRequest("Código de administrador inválido.");
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var roleName = role.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
            await _roleManager.CreateAsync(new IdentityRole(roleName));

        await _userManager.AddToRoleAsync(user, roleName);

        return Ok(new { message = "Usuário criado com sucesso" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return Unauthorized("Email ou senha inválidos");

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);

        return Ok(new { token });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Ok(new { message = "Se o email existir, você receberá um link de redefinição." });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        // Em produção: enviar email com link contendo token
        return Ok(new { token, message = "Use este token para redefinir sua senha." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDTO model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return BadRequest("Usuário não encontrado.");

        var isAdmin = await _userManager.IsInRoleAsync(user, UserRole.Admin.ToString());
        if (isAdmin)
        {
            var expectedCode = Environment.GetEnvironmentVariable("ADMIN_REGISTRATION_CODE") ?? "Admin@123";
            if (string.IsNullOrEmpty(model.AdminCode) || model.AdminCode != expectedCode)
                return BadRequest("Código de administrador inválido para redefinir senha.");
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Senha redefinida com sucesso." });
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id!),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(ClaimTypes.NameIdentifier, user.Id!)
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
            throw new InvalidOperationException("JWT Key not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
