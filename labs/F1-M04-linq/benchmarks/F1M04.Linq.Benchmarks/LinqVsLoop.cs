using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace F1M04.Linq.Benchmarks;

/// <summary>
/// Soma o preço dos produtos ativos de uma categoria: LINQ x foreach x Span.
/// Hipótese a testar: LINQ aloca (delegates/iteradores) e é um pouco mais lento,
/// mas a diferença só importa em hot paths com coleções grandes.
/// </summary>
[MemoryDiagnoser]
public class LinqVsLoop
{
    private List<Produto> _produtos = [];

    [Params(100, 10_000)]
    public int Quantidade { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string[] categorias = ["Livros", "Eletrônicos", "Casa"];
        var random = new Random(42); // semente fixa: resultados reprodutíveis
        _produtos = Enumerable.Range(1, Quantidade)
            .Select(i => new Produto(i, $"SKU-{i:D6}", $"Produto {i}", categorias[i % 3], random.Next(10, 1000), i % 5 != 0))
            .ToList();
    }

    [Benchmark(Baseline = true)]
    public decimal Linq() =>
        _produtos.Where(p => p.Ativo && p.Categoria == "Livros").Sum(p => p.Preco);

    [Benchmark]
    public decimal Foreach()
    {
        decimal total = 0;
        foreach (var p in _produtos)
            if (p.Ativo && p.Categoria == "Livros")
                total += p.Preco;
        return total;
    }

    [Benchmark]
    public decimal Span()
    {
        decimal total = 0;
        foreach (var p in CollectionsMarshal.AsSpan(_produtos))
            if (p.Ativo && p.Categoria == "Livros")
                total += p.Preco;
        return total;
    }

    /// <summary>Armadilha clássica: Count() e depois ToList() na MESMA consulta = 2 enumerações.</summary>
    [Benchmark]
    public int LinqEnumerandoDuasVezes()
    {
        var consulta = _produtos.Where(p => p.Ativo).OrderBy(p => p.Preco);
        var total = consulta.Count();
        var primeiros = consulta.Take(20).ToList();
        return total + primeiros.Count;
    }

    [Benchmark]
    public int LinqMaterializandoUmaVez()
    {
        var lista = _produtos.Where(p => p.Ativo).OrderBy(p => p.Preco).ToList();
        var primeiros = lista.Take(20).ToList();
        return lista.Count + primeiros.Count;
    }
}
