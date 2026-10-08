using System.Collections.Concurrent;

namespace F1M06.Hosting;

/// <summary>Produto do catálogo.</summary>
public sealed record Produto(string Sku, string Nome, decimal Preco);

/// <summary>Acesso aos produtos.</summary>
public interface IRepositorioProdutos
{
    /// <summary>Quantas consultas foram feitas (para os testes enxergarem o efeito do cache).</summary>
    int Consultas { get; }

    Produto? ObterPorSku(string sku);
}

/// <summary>
/// Repositório em memória. Guarda ESTADO compartilhado e é thread-safe → candidato natural a Singleton.
/// </summary>
public sealed class RepositorioProdutosEmMemoria : IRepositorioProdutos
{
    private readonly ConcurrentDictionary<string, Produto> _produtos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SKU-1"] = new("SKU-1", "Livro Clean Code", 100m),
        ["SKU-2"] = new("SKU-2", "Teclado mecânico", 250m),
    };

    private int _consultas;

    public int Consultas => Volatile.Read(ref _consultas);

    public Produto? ObterPorSku(string sku)
    {
        Interlocked.Increment(ref _consultas);
        return _produtos.GetValueOrDefault(sku);
    }
}

/// <summary>Consulta de preços.</summary>
public interface IServicoDePrecos
{
    /// <summary>Preço atual do SKU. Lança <see cref="KeyNotFoundException"/> se não existir.</summary>
    decimal ObterPreco(string sku);
}

/// <summary>Implementação "real": vai ao repositório a cada chamada.</summary>
public sealed class ServicoDePrecos(IRepositorioProdutos repositorio) : IServicoDePrecos
{
    public decimal ObterPreco(string sku) =>
        repositorio.ObterPorSku(sku)?.Preco ?? throw new KeyNotFoundException($"Produto '{sku}' não encontrado.");
}
