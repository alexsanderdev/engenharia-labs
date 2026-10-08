namespace F4M01.Domain.Pedidos;

/// <summary>Linha do pedido. Preço unitário congelado no momento da compra.</summary>
/// <remarks>TODO (Passo 2): <c>private set</c> em tudo — item de pedido não muda por atribuição de fora do agregado.</remarks>
public sealed class ItemPedido
{
    public ItemPedido(Guid produtoId, string nomeProduto, int quantidade, decimal precoUnitario)
    {
        ProdutoId = produtoId;
        NomeProduto = nomeProduto;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }

    public Guid ProdutoId { get; set; }
    public string NomeProduto { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;
}
