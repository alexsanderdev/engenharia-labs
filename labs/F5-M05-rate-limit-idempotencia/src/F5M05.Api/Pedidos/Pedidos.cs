using System.Collections.Concurrent;

namespace F5M05.Api.Pedidos;

// ARQUIVO PRONTO — não precisa alterar.

public sealed record ItemPedidoRequest(Guid ProdutoId, int Quantidade);

public sealed record CriarPedidoRequest(IReadOnlyList<ItemPedidoRequest>? Itens);

public sealed record ItemPedidoResponse(Guid ProdutoId, int Quantidade, decimal PrecoUnitario);

public sealed record PedidoResponse(Guid Id, string ClienteId, IReadOnlyList<ItemPedidoResponse> Itens, decimal Total, DateTimeOffset CriadoEm);

public sealed record Pedido(Guid Id, string ClienteId, IReadOnlyList<ItemPedidoResponse> Itens, decimal Total, DateTimeOffset CriadoEm)
{
    public PedidoResponse ParaResposta() => new(Id, ClienteId, Itens, Total, CriadoEm);
}

public interface IRepositorioDePedidos
{
    /// <summary>Quantos pedidos existem — o teste de idempotência usa para provar "nenhum duplicado".</summary>
    int Quantidade { get; }

    Task AdicionarAsync(Pedido pedido, CancellationToken ct);
    Task<Pedido?> ObterAsync(Guid id, CancellationToken ct);
}

public sealed class RepositorioDePedidosEmMemoria : IRepositorioDePedidos
{
    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();

    public int Quantidade => _pedidos.Count;

    public Task AdicionarAsync(Pedido pedido, CancellationToken ct)
    {
        _pedidos[pedido.Id] = pedido;
        return Task.CompletedTask;
    }

    public Task<Pedido?> ObterAsync(Guid id, CancellationToken ct) => Task.FromResult(_pedidos.GetValueOrDefault(id));
}
