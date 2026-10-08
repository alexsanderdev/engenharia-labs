namespace F3M04.EfCore.Dominio;

/// <summary>
/// Item de um pedido. Guarda o preço do produto NO MOMENTO da compra (snapshot):
/// se o preço do catálogo mudar depois, o pedido não muda.
/// </summary>
public sealed class ItemPedido
{
    /// <summary>Construtor usado pelo EF Core na materialização.</summary>
    private ItemPedido() { }

    internal ItemPedido(Guid produtoId, int quantidade, decimal precoUnitario)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        Id = Guid.CreateVersion7();
        ProdutoId = produtoId;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }

    public Guid Id { get; private set; }
    public Guid ProdutoId { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }

    /// <summary>Calculado em memória: não é coluna (propriedade só com get não é mapeada por convenção).</summary>
    public decimal Subtotal => Quantidade * PrecoUnitario;
}
