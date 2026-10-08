namespace F2M08.Domain.Entidades;

/// <remarks>TODO (Passo 3): <c>Quantidade</c> e <c>PrecoUnitario</c> não podem ser alterados de fora do agregado.</remarks>
public sealed class ItemPedido
{
    public ItemPedido(Guid produtoId, int quantidade, decimal precoUnitario)
    {
        ProdutoId = produtoId;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }

    public Guid ProdutoId { get; private set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;

    internal void Somar(int quantidade) => Quantidade += quantidade;
}
