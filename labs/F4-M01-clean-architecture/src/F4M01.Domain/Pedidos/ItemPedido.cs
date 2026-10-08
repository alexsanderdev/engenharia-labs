namespace F4M01.Domain.Pedidos;

/// <summary>Linha do pedido. Preço unitário congelado no momento da compra.</summary>
public sealed class ItemPedido
{
    public ItemPedido(Guid produtoId, string nomeProduto, int quantidade, decimal precoUnitario)
    {
        ProdutoId = produtoId;
        NomeProduto = nomeProduto;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }

    public Guid ProdutoId { get; private set; }
    public string NomeProduto { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;
}
