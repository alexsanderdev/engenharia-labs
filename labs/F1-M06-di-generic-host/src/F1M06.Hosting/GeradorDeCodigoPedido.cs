using System.Globalization;

namespace F1M06.Hosting;

/// <summary>
/// Gerador com estado (contador) → Singleton. Não tem construtor "resolvível" pelo container
/// (recebe uma string), por isso é registrado com uma FACTORY que lê as opções.
/// </summary>
public sealed class GeradorDeCodigoPedido(string prefixo, TimeProvider tempo) : IGeradorDeCodigoPedido
{
    private int _sequencial;

    /// <summary>
    /// Devolve <c>{prefixo}-{ano UTC atual do TimeProvider, 4 dígitos}-{sequencial, 6 dígitos}</c>,
    /// começando em 000001. Deve ser thread-safe (use <see cref="Interlocked"/>).
    /// </summary>
    public string Proximo()
    {
        var numero = Interlocked.Increment(ref _sequencial);
        var ano = tempo.GetUtcNow().Year;
        return string.Create(CultureInfo.InvariantCulture, $"{prefixo}-{ano:D4}-{numero:D6}");
    }
}
