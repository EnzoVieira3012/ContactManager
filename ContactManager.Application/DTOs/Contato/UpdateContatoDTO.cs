namespace ContactManager.Application.DTOs.Contato;

public class UpdateContatoDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public string Sexo { get; set; } = string.Empty;
}
