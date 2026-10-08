using System.Buffers;

namespace F1M05.Memoria;

/// <summary>
/// Monta etiquetas de expedição, escolhendo entre <c>stackalloc</c> (textos pequenos) e
/// <see cref="ArrayPool{T}"/> (textos grandes) para o buffer temporário.
/// </summary>
public static class Etiquetas
{
    /// <summary>
    /// Até este tamanho (em chars) o buffer temporário vai na stack. Acima disso, usa o pool:
    /// stack é pequena (~1 MB por thread) e um <c>stackalloc</c> grande pode derrubar o processo.
    /// </summary>
    public const int LimiteStackalloc = 256;

    /// <summary>
    /// Monta <c>"PED-2026-000123 | ANA SILVA"</c>: código formatado, " | " e o nome do cliente
    /// sem espaços nas pontas e em maiúsculas (invariant).
    /// Se o tamanho total for &lt;= <see cref="LimiteStackalloc"/>, use <c>stackalloc</c> e NÃO toque no pool.
    /// Caso contrário, alugue de <paramref name="pool"/> (padrão: <see cref="ArrayPool{T}.Shared"/>) e
    /// devolva SEMPRE (try/finally). A única alocação esperada é a string final.
    /// </summary>
    public static string MontarEtiqueta(CodigoPedido codigo, ReadOnlySpan<char> nomeCliente, ArrayPool<char>? pool = null)
    {
        nomeCliente = nomeCliente.Trim();
        var tamanho = CodigoPedido.Tamanho + 3 + nomeCliente.Length;

        if (tamanho <= LimiteStackalloc)
        {
            Span<char> buffer = stackalloc char[tamanho];
            Escrever(codigo, nomeCliente, buffer);
            return new string(buffer);
        }

        pool ??= ArrayPool<char>.Shared;
        var alugado = pool.Rent(tamanho); // pode vir MAIOR do que o pedido
        try
        {
            var buffer = alugado.AsSpan(0, tamanho);
            Escrever(codigo, nomeCliente, buffer);
            return new string(buffer);
        }
        finally
        {
            pool.Return(alugado);
        }
    }

    private static void Escrever(CodigoPedido codigo, ReadOnlySpan<char> nome, Span<char> destino)
    {
        ParserCodigoPedido.TryFormat(codigo, destino, out var escritos);
        " | ".AsSpan().CopyTo(destino[escritos..]);
        nome.ToUpperInvariant(destino[(escritos + 3)..]);
    }
}
