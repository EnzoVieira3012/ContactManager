using ContactManager.Application.Services;
using ContactManager.Application.Interfaces;
using ContactManager.Domain.Interfaces;
using ContactManager.Infrastructure.Data;
using ContactManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ContactManager.Domain.Entities;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Carrega variáveis do .env
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
            Environment.SetEnvironmentVariable(parts[0], parts[1]);
    }
}

// Log para depuração
var jwtKeyFromEnv = Environment.GetEnvironmentVariable("JWT_KEY");
Console.WriteLine($"JWT_KEY from env: {jwtKeyFromEnv}");

// Connection string com placeholders
var connStringTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connStringTemplate))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

var finalConnString = connStringTemplate;
foreach (var envVar in Environment.GetEnvironmentVariables().Keys)
{
    var key = envVar.ToString();
    if (string.IsNullOrEmpty(key)) continue;
    var pattern = $"${{{key}}}";
    if (finalConnString.Contains(pattern))
    {
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            finalConnString = finalConnString.Replace(pattern, envValue);
        }
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(finalConnString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Obtém a chave JWT diretamente do ambiente
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
if (string.IsNullOrEmpty(jwtKey))
{
    // Fallback seguro para desenvolvimento (não use em produção)
    jwtKey = "uma-chave-muito-longa-com-pelo-menos-32-caracteres-para-testes";
    Console.WriteLine("WARNING: JWT_KEY não encontrada no ambiente. Usando chave padrão (apenas para testes).");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT no formato: Bearer {seu-token}"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddScoped<IContatoRepository, ContatoRepository>();
builder.Services.AddScoped<IContatoService, ContatoService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    api = "ContactManager API",
    version = "1.0",
    endpoints = new[] { "/health", "/swagger", "/api/Contato" },
    message = "Bem-vindo à API de gerenciamento de contatos. Use /swagger para documentação."
}))
.WithName("Root");

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

app.Run();
