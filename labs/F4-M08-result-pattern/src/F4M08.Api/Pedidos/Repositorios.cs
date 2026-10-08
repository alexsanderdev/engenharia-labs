using System.Collections.Concurrent;

namespace F4M08.Api.Pedidos;

/// <summary>Produtos fixos do catálogo usados no lab e nos testes.</summary>
public static class ProdutosConhecidos
{
    public static readonly Produto Teclado = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Teclado mecânico", 250.00m, Ativo: true);
    public static readonly Produto Mouse = new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Mouse sem fio", 120.00m, Ativo: true);
    public static readonly Produto Webcam = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Webcam HD", 300.00m, Ativo: false);
}

public interface ICatalogo
{
    Task<Produto?> ObterAsync(Guid produtoId, CancellationToken ct);
}

public interface IPedidoRepositorio
{
    Task<Pedido?> ObterAsync(Guid pedidoId, CancellationToken ct);
    Task AdicionarAsync(Pedido pedido, CancellationToken ct);
}

/// <summary>Catálogo em memória (PRONTO).</summary>
public sealed class CatalogoEmMemoria : ICatalogo
{
    private readonly Dictionary<Guid, Produto> _produtos = new[]
    {
        ProdutosConhecidos.Teclado, ProdutosConhecidos.Mouse, ProdutosConhecidos.Webcam,
    }.ToDictionary(p => p.Id);

    public Task<Produto?> ObterAsync(Guid produtoId, CancellationToken ct) =>
        Task.FromResult(_produtos.GetValueOrDefault(produtoId));
}

/// <summary>Repositório em memória (PRONTO). Singleton: a "persistência" vive enquanto a aplicação vive.</summary>
public sealed class PedidoRepositorioEmMemoria : IPedidoRepositorio
{
    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();

    public Task<Pedido?> ObterAsync(Guid pedidoId, CancellationToken ct) =>
        Task.FromResult(_pedidos.GetValueOrDefault(pedidoId));

    public Task AdicionarAsync(Pedido pedido, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        _pedidos[pedido.Id] = pedido;
        return Task.CompletedTask;
    }
}
