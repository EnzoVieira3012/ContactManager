namespace ContactManager.Domain.Entities;

public class Contato
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public int Idade => CalcularIdade();

    private int CalcularIdade()
    {
        var today = DateTime.Today;
        var age = today.Year - DataNascimento.Year;
        if (DataNascimento.Date > today.AddYears(-age)) age--;
        return age;
    }
}
