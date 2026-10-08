namespace F2M08.Domain.Entidades;

public sealed class ItemPedido
{
    public ItemPedido(Guid produtoId, int quantidade, decimal precoUnitario)
    {
        ProdutoId = produtoId;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }

    public Guid ProdutoId { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;

    internal void Somar(int quantidade) => Quantidade += quantidade;
}
