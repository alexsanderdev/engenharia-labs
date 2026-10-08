using System.Collections.Concurrent;
using F4M04.Cqrs.Pedidos.Dominio;

namespace F4M04.Cqrs.Pedidos.Infra;

/// <summary>Produtos fixos do catálogo usados no lab e nos testes.</summary>
public static class ProdutosConhecidos
{
    public static readonly Produto Teclado = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Teclado mecânico", 250.00m, Ativo: true);
    public static readonly Produto Mouse = new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Mouse sem fio", 120.00m, Ativo: true);
    public static readonly Produto Webcam = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Webcam HD", 300.00m, Ativo: false);

    public static IEnumerable<Produto> Todos => [Teclado, Mouse, Webcam];
}

/// <summary>
/// "Banco" do lado de ESCRITA (singleton): agregados confirmados e o catálogo.
/// Faz o papel das tabelas normalizadas que o EF Core gravaria.
/// </summary>
public sealed class BancoDeEscrita
{
    private int _commits;

    public ConcurrentDictionary<Guid, Pedido> Pedidos { get; } = new();

    public ConcurrentDictionary<Guid, Produto> Produtos { get; } =
        new(ProdutosConhecidos.Todos.ToDictionary(p => p.Id));

    /// <summary>Quantos commits já aconteceram (útil para provar que queries não gravam).</summary>
    public int Commits => _commits;

    internal void RegistrarCommit() => Interlocked.Increment(ref _commits);
}
