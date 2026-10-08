using System.Collections.Concurrent;

namespace F5M05.Api.Catalogo;

// ARQUIVO PRONTO — não precisa alterar.
// Catálogo em memória que CONTA quantas vezes foi lido — é assim que os testes
// enxergam se a resposta veio do output cache ou do "banco".

public sealed record Produto(Guid Id, string Nome, string Categoria, decimal Preco);

public interface ICatalogo
{
    /// <summary>Quantas vezes o catálogo foi consultado (simula ida ao banco).</summary>
    int Leituras { get; }

    Task<IReadOnlyList<Produto>> ListarAsync(string? categoria, CancellationToken ct);
    Task<Produto?> ObterAsync(Guid id, CancellationToken ct);
    Task<bool> AtualizarPrecoAsync(Guid id, decimal preco, CancellationToken ct);
}

public static class ProdutosConhecidos
{
    public static readonly Guid TecladoId = Guid.Parse("55555555-0000-0000-0000-000000000001");
    public static readonly Guid MouseId = Guid.Parse("55555555-0000-0000-0000-000000000002");
    public static readonly Guid MonitorId = Guid.Parse("55555555-0000-0000-0000-000000000003");
}

public sealed class CatalogoEmMemoria : ICatalogo
{
    private readonly ConcurrentDictionary<Guid, Produto> _produtos = new(new Dictionary<Guid, Produto>
    {
        [ProdutosConhecidos.TecladoId] = new(ProdutosConhecidos.TecladoId, "Teclado mecânico", "perifericos", 199.90m),
        [ProdutosConhecidos.MouseId] = new(ProdutosConhecidos.MouseId, "Mouse sem fio", "perifericos", 99.90m),
        [ProdutosConhecidos.MonitorId] = new(ProdutosConhecidos.MonitorId, "Monitor 27\"", "monitores", 1299.00m),
    });

    private int _leituras;

    public int Leituras => Volatile.Read(ref _leituras);

    public Task<IReadOnlyList<Produto>> ListarAsync(string? categoria, CancellationToken ct)
    {
        Interlocked.Increment(ref _leituras);
        IReadOnlyList<Produto> lista = [.. _produtos.Values
            .Where(p => categoria is null || p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Nome)];
        return Task.FromResult(lista);
    }

    public Task<Produto?> ObterAsync(Guid id, CancellationToken ct) => Task.FromResult(_produtos.GetValueOrDefault(id));

    public Task<bool> AtualizarPrecoAsync(Guid id, decimal preco, CancellationToken ct)
    {
        if (!_produtos.TryGetValue(id, out var atual)) return Task.FromResult(false);
        _produtos[id] = atual with { Preco = preco };
        return Task.FromResult(true);
    }
}
