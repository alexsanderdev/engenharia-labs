using F2M07.Api.Produtos;

namespace F2M07.Api.Pedidos;

/// <summary>Created → Confirmed → Completed; Created → Cancelled. Completed não cancela.</summary>
public enum StatusPedido
{
    Created = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3,
}

/// <summary>
/// Agregado Pedido. O total é SEMPRE calculado aqui (no servidor), a partir
/// do preço do produto no momento da compra (snapshot em <see cref="ItemPedido.PrecoUnitario"/>).
/// </summary>
public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    private Pedido() { }

    public Pedido(Guid id, Guid clienteId, DateTimeOffset criadoEm)
    {
        Id = id;
        ClienteId = clienteId;
        CriadoEm = criadoEm;
        Status = StatusPedido.Created;
    }

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public StatusPedido Status { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyList<ItemPedido> Itens => _itens;

    public void AdicionarItem(Produto produto, int quantidade)
    {
        ArgumentNullException.ThrowIfNull(produto);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        if (!produto.Ativo)
            throw new InvalidOperationException($"O produto {produto.Id} está inativo e não pode entrar em pedido novo.");

        _itens.Add(new ItemPedido(Guid.NewGuid(), produto.Id, quantidade, produto.Preco));
        Total = _itens.Sum(i => i.Subtotal);
    }

    public bool PodeCancelar => Status == StatusPedido.Created;

    public void Confirmar() => Transicionar(StatusPedido.Created, StatusPedido.Confirmed);

    public void Concluir() => Transicionar(StatusPedido.Confirmed, StatusPedido.Completed);

    public void Cancelar() => Transicionar(StatusPedido.Created, StatusPedido.Cancelled);

    private void Transicionar(StatusPedido de, StatusPedido para)
    {
        if (Status != de)
            throw new InvalidOperationException($"Transição inválida: {Status} → {para}.");
        Status = para;
    }
}

public sealed class ItemPedido
{
    private ItemPedido() { }

    public ItemPedido(Guid id, Guid produtoId, int quantidade, decimal precoUnitario)
    {
        Id = id;
        ProdutoId = produtoId;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }

    public Guid Id { get; private set; }
    public Guid ProdutoId { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;
}
