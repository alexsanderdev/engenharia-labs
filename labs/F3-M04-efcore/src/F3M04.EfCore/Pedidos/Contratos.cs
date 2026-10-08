using F3M04.EfCore.Dominio;

namespace F3M04.EfCore.Pedidos;

/// <summary>Item pedido pelo cliente: só o produto e a quantidade (o preço vem do banco).</summary>
public sealed record ItemSolicitado(Guid ProdutoId, int Quantidade);

/// <summary>Detalhe de um pedido para a tela/API: já com nomes, sem entidades do EF.</summary>
public sealed record PedidoResumoDto(
    Guid Id,
    string ClienteNome,
    StatusPedido Status,
    decimal Total,
    DateTimeOffset CriadoEm,
    IReadOnlyList<ItemResumoDto> Itens);

/// <summary>Item do <see cref="PedidoResumoDto"/>.</summary>
public sealed record ItemResumoDto(string ProdutoNome, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

/// <summary>Linha da listagem "meus pedidos".</summary>
public sealed record PedidoListaDto(Guid Id, DateTimeOffset CriadoEm, StatusPedido Status, int QuantidadeDeItens, decimal Total);
