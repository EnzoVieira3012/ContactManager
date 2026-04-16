namespace ContactManager.Application.DTOs;

public class CreateContatoDTO
{
    public string Nome { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public string Sexo { get; set; } = string.Empty;
}
