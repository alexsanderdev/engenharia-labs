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
    public string Formatado => throw new NotImplementedException("TODO: devolva Numero no formato 000.000.000-00.");

    /// <summary>
    /// Cria um CPF a partir de texto com ou sem máscara.
    /// Regras: 11 dígitos, não pode ter todos os dígitos iguais e os dois dígitos verificadores devem bater.
    /// </summary>
    /// <exception cref="ArgumentException">CPF inválido.</exception>
    public static Cpf Criar(string? valor) =>
        throw new NotImplementedException("TODO: mova para cá a validação de CPF de CadastroDeClientes.Cadastrar (aceite null).");

    /// <inheritdoc />
    public override string ToString() => Numero;
}
