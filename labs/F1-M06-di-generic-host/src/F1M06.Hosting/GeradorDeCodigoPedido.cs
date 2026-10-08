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
        // Dica: string.Create(CultureInfo.InvariantCulture, $"...{numero:D6}") evita depender da cultura da máquina.
        throw new NotImplementedException("TODO: Interlocked.Increment(ref _sequencial) + ano de tempo.GetUtcNow() + formato {prefixo}-AAAA-NNNNNN");
    }
}
