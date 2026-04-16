using ContactManager.Application.Services;
using ContactManager.Application.Interfaces;
using ContactManager.Domain.Interfaces;
using ContactManager.Infrastructure.Data;
using ContactManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { 
    api = "ContactManager API",
    version = "1.0",
    endpoints = new[] { "/health", "/swagger", "/api/Contato" },
    message = "Bem-vindo à API de gerenciamento de contatos. Use /swagger para documentação."
}))
.WithName("Root");

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

app.Run();
