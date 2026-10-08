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
    /// que não começa nem termina com '.'.
    /// </summary>
    /// <exception cref="ArgumentException">E-mail inválido.</exception>
    public static Email Criar(string? valor)
    {
        var normalizado = (valor ?? string.Empty).Trim().ToLowerInvariant();
        var partes = normalizado.Split('@');

        var valido = partes.Length == 2
            && partes[0].Length > 0
            && partes[1].Contains('.')
            && !partes[1].StartsWith('.')
            && !partes[1].EndsWith('.')
            && !normalizado.Contains(' ');

        return valido ? new Email(normalizado) : throw new ArgumentException("E-mail inválido.", nameof(valor));
    }

    /// <inheritdoc />
    public override string ToString() => Endereco;
}
