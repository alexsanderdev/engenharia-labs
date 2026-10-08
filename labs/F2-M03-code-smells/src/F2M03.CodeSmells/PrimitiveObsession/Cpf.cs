namespace F2M03.CodeSmells.PrimitiveObsession;

/// <summary>
/// Value object de CPF: só existe se for válido, guarda apenas os 11 dígitos e se compara por valor.
/// </summary>
public sealed record Cpf
{
    /// <summary>Os 11 dígitos, sem máscara.</summary>
    public string Numero { get; }

    private Cpf(string numero) => Numero = numero;

    /// <summary>CPF no formato 000.000.000-00.</summary>
    public string Formatado => $"{Numero[..3]}.{Numero[3..6]}.{Numero[6..9]}-{Numero[9..]}";

    /// <summary>
    /// Cria um CPF a partir de texto com ou sem máscara.
    /// Regras: 11 dígitos, não pode ter todos os dígitos iguais e os dois dígitos verificadores devem bater.
    /// </summary>
    /// <exception cref="ArgumentException">CPF inválido.</exception>
    public static Cpf Criar(string? valor)
    {
        var digitos = new string((valor ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

        if (digitos.Length != 11 || digitos.Distinct().Count() == 1)
            throw new ArgumentException("CPF inválido.", nameof(valor));

        if (digitos[9] - '0' != DigitoVerificador(digitos, 9) || digitos[10] - '0' != DigitoVerificador(digitos, 10))
            throw new ArgumentException("CPF inválido.", nameof(valor));

        return new Cpf(digitos);
    }

    /// <inheritdoc />
    public override string ToString() => Formatado;

    private static int DigitoVerificador(string digitos, int quantidade)
    {
        var soma = 0;
        for (var i = 0; i < quantidade; i++)
            soma += (digitos[i] - '0') * (quantidade + 1 - i);

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
