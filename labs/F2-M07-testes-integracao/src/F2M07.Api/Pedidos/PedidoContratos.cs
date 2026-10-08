namespace F2M07.Api.Pedidos;

/// <summary>Entrada de POST /pedidos. Repare: o cliente NÃO manda preço nem total.</summary>
public sealed record CriarPedidoRequest(IReadOnlyList<ItemPedidoRequest>? Itens);

public sealed record ItemPedidoRequest(Guid ProdutoId, int Quantidade);

public sealed record ItemPedidoResponse(Guid ProdutoId, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

public sealed record PedidoResponse(
    Guid Id,
    Guid ClienteId,
    string Status,
    decimal Total,
    DateTimeOffset CriadoEm,
    IReadOnlyList<ItemPedidoResponse> Itens)
{
    public static PedidoResponse De(Pedido p) => new(
        p.Id,
        p.ClienteId,
        p.Status.ToString(),
        p.Total,
        p.CriadoEm,
        [.. p.Itens.Select(i => new ItemPedidoResponse(i.ProdutoId, i.Quantidade, i.PrecoUnitario, i.Subtotal))]);
}
