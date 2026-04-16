namespace ContactManager.Application.Validators;

public static class ContatoValidator
{
    public static void Validate(DateTime dataNascimento)
    {
        var today = DateTime.Today;
        if (dataNascimento > today)
            throw new ArgumentException("Data de nascimento não pode ser maior que a data de hoje.");

        var idade = CalcularIdade(dataNascimento);
        if (idade <= 0)
            throw new ArgumentException("Idade não pode ser igual ou menor que 0.");
        if (idade < 18)
            throw new ArgumentException("O contato deve ser maior de idade (18 anos ou mais).");
    }

    private static int CalcularIdade(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
