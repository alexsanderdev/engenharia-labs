using System.ComponentModel;
using F5M02.Api.Dominio;

namespace F5M02.Api.Contratos;

// ---------------------------------------------------------------------------------------------
// Contratos PRONTOS. A v2 tem DUAS mudanças incompatíveis (breaking changes) em relação à v1:
//   1) "status" era número (enum serializado como int) e passa a ser texto ("Confirmed");
//   2) "total" (decimal solto, moeda implícita) vira "valorTotal": { "valor": 620.00, "moeda": "BRL" }.
// Um cliente da v1 que leia a resposta da v2 quebra — por isso a v2 é uma NOVA versão, e a v1 continua
// existindo (depreciada) até a data de sunset.
// ---------------------------------------------------------------------------------------------

/// <summary>Contrato v1 (legado). Mantido como está: mudar qualquer campo quebraria quem ainda usa a v1.</summary>
public sealed record PedidoV1Response(
    [property: Description("Identificador do pedido.")] Guid Id,
    [property: Description("Identificador do cliente.")] Guid ClienteId,
    [property: Description("Status numérico (0 = Created, 1 = Confirmed, 2 = Completed, 3 = Cancelled).")] int Status,
    [property: Description("Total do pedido em reais.")] decimal Total,
    [property: Description("Quantidade de itens distintos.")] int QuantidadeItens)
{
    public static PedidoV1Response De(Pedido p) => new(p.Id, p.ClienteId, (int)p.Status, p.Total, p.Itens.Count);
}

/// <summary>Valor monetário com moeda explícita (ISO 4217).</summary>
public sealed record DinheiroResponse(
    [property: Description("Valor com duas casas decimais.")] decimal Valor,
    [property: Description("Código ISO 4217 da moeda.")] string Moeda);

public sealed record ItemPedidoV2Response(Guid ProdutoId, string Nome, int Quantidade, DinheiroResponse PrecoUnitario, DinheiroResponse Subtotal);

/// <summary>Contrato v2.</summary>
public sealed record PedidoV2Response(
    [property: Description("Identificador do pedido.")] Guid Id,
    [property: Description("Identificador do cliente.")] Guid ClienteId,
    [property: Description("Status textual: Created, Confirmed, Completed ou Cancelled.")] string Status,
    [property: Description("Data/hora de criação (UTC).")] DateTimeOffset CriadoEm,
    [property: Description("Valor total com moeda explícita.")] DinheiroResponse ValorTotal,
    [property: Description("Itens do pedido.")] IReadOnlyList<ItemPedidoV2Response> Itens)
{
    public const string Moeda = "BRL";

    public static PedidoV2Response De(Pedido p) => new(
        p.Id,
        p.ClienteId,
        p.Status.ToString(),
        p.CriadoEm,
        new DinheiroResponse(p.Total, Moeda),
        [.. p.Itens.Select(i => new ItemPedidoV2Response(
            i.ProdutoId, i.NomeProduto, i.Quantidade, new DinheiroResponse(i.PrecoUnitario, Moeda), new DinheiroResponse(i.Subtotal, Moeda)))]);
}

/// <summary>Corpo do <c>POST /v2/pedidos</c>.</summary>
public sealed record CriarPedidoV2Request(
    [property: Description("Cliente que está comprando.")] Guid ClienteId,
    [property: Description("Itens (pelo menos um).")] List<ItemPedidoV2Request>? Itens);

public sealed record ItemPedidoV2Request(
    [property: Description("Produto do catálogo.")] Guid ProdutoId,
    [property: Description("Quantidade (maior que zero).")] int Quantidade);
