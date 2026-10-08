namespace F2M03.CodeSmells.PrimitiveObsession;

/// <summary>
/// Value object de e-mail: normalizado (sem espaços nas pontas, minúsculo) e com formato mínimo válido.
/// </summary>
public sealed record Email
{
    /// <summary>Endereço normalizado.</summary>
    public string Endereco { get; }

    private Email(string endereco) => Endereco = endereco;

    /// <summary>
    /// Cria um e-mail. Regras: exatamente um '@', parte local não vazia, domínio com '.'
    /// que não começa nem termina com '.', sem espaços internos.
    /// </summary>
    /// <exception cref="ArgumentException">E-mail inválido.</exception>
    public static Email Criar(string? valor) =>
        throw new NotImplementedException("TODO: mova para cá a normalização e a validação de e-mail de CadastroDeClientes.Cadastrar.");

    /// <inheritdoc />
    public override string ToString() => Endereco;
}
