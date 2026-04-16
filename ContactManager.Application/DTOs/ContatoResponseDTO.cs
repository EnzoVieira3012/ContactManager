namespace ContactManager.Application.DTOs;

public class ContatoResponseDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public int Idade { get; set; }
    public bool IsActive { get; set; }
}
