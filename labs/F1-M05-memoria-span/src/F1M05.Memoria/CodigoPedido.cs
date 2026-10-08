namespace F1M05.Memoria;

/// <summary>Tipo do documento identificado pelo código.</summary>
public enum TipoCodigo
{
    /// <summary>Prefixo "PED".</summary>
    Pedido,
    /// <summary>Prefixo "DEV".</summary>
    Devolucao,
}

/// <summary>
/// Código de pedido no formato <c>PPP-AAAA-NNNNNN</c>, por exemplo <c>PED-2026-000123</c>.
/// É um <c>readonly record struct</c>: value type pequeno (12 bytes), imutável, com igualdade
/// por valor gerada pelo compilador (implementa <see cref="IEquatable{T}"/> — sem boxing em
/// Dictionary/HashSet).
/// </summary>
public readonly record struct CodigoPedido(TipoCodigo Tipo, int Ano, int Sequencial)
{
    /// <summary>Quantidade de caracteres do código formatado.</summary>
    public const int Tamanho = 15;

    /// <summary>"PED" ou "DEV".</summary>
    public string Prefixo => Tipo == TipoCodigo.Pedido ? "PED" : "DEV";

    /// <summary>
    /// Formata usando um buffer na stack (<c>stackalloc</c>); a única alocação é a string final.
    /// </summary>
    public override string ToString()
    {
        Span<char> buffer = stackalloc char[Tamanho];
        return ParserCodigoPedido.TryFormat(this, buffer, out var escritos)
            ? new string(buffer[..escritos])
            : string.Empty;
    }
}
