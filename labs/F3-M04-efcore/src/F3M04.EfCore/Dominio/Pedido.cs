namespace F3M04.EfCore.Dominio;

/// <summary>
/// Pedido do OrderFlow (raiz do agregado Pedido → Itens). O total é sempre calculado
/// aqui, a partir dos itens; nunca vem de fora.
/// </summary>
public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    /// <summary>Construtor usado pelo EF Core na materialização.</summary>
    private Pedido() { }

    public Pedido(Guid clienteId, DateTimeOffset criadoEm)
    {
        Id = Guid.CreateVersion7();
        ClienteId = clienteId;
        CriadoEm = criadoEm;
        Status = StatusPedido.Created;
    }

    public Guid Id { get; private set; }

    /// <summary>Referência ao cliente por id (o agregado Pedido não carrega o agregado Cliente).</summary>
    public Guid ClienteId { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }
    public StatusPedido Status { get; private set; }
    public decimal Total { get; private set; }

    /// <summary>
    /// Coleção só de leitura para quem está fora. O EF Core encontra o campo <c>_itens</c>
    /// por convenção e escreve nele direto.
    /// </summary>
    public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();

    public void AdicionarItem(Produto produto, int quantidade)
    {
        ArgumentNullException.ThrowIfNull(produto);
        if (Status != StatusPedido.Created)
            throw new InvalidOperationException("Só é possível alterar itens de pedido em Created.");
        if (!produto.Ativo)
            throw new InvalidOperationException($"O produto {produto.Sku} está inativo e não pode entrar em pedido.");

        _itens.Add(new ItemPedido(produto.Id, quantidade, produto.Preco));
        Total = _itens.Sum(i => i.Subtotal);
    }

    public void Confirmar()
    {
        if (Status != StatusPedido.Created)
            throw new InvalidOperationException($"Pedido em {Status} não pode ser confirmado.");
        if (_itens.Count == 0)
            throw new InvalidOperationException("Pedido sem itens não pode ser confirmado.");
        Status = StatusPedido.Confirmed;
    }

    public void Concluir()
    {
        if (Status != StatusPedido.Confirmed)
            throw new InvalidOperationException($"Pedido em {Status} não pode ser concluído.");
        Status = StatusPedido.Completed;
    }

    public void Cancelar()
    {
        if (Status is not StatusPedido.Created)
            throw new InvalidOperationException($"Pedido em {Status} não pode ser cancelado.");
        Status = StatusPedido.Cancelled;
    }
}
