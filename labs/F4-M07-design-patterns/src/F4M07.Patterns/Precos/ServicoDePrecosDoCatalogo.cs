using System.Collections.Concurrent;

namespace F4M07.Patterns.Precos;

/// <summary>
/// Implementação "real" (simula a consulta ao banco do Catálogo). PRONTA — não altere.
/// Conta as consultas para os testes provarem que o cache funciona.
/// </summary>
public sealed class ServicoDePrecosDoCatalogo : IServicoDePrecos
{
    private readonly ConcurrentDictionary<string, decimal> _precos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SKU-CAFE"] = 39.90m,
        ["SKU-CANECA"] = 59.90m,
        ["SKU-MOEDOR"] = 349.00m,
    };

    private int _consultas;

    /// <summary>Quantas vezes o "banco" foi consultado.</summary>
    public int Consultas => _consultas;

    /// <summary>Simula uma alteração de preço feita pelo time comercial.</summary>
    public void AlterarPreco(string sku, decimal novoPreco) => _precos[sku] = novoPreco;

    public ValueTask<decimal?> ObterPrecoAsync(string sku, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _consultas);
        return ValueTask.FromResult<decimal?>(_precos.TryGetValue(sku, out var preco) ? preco : null);
    }
}
