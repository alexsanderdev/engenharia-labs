namespace F4M06.Pedidos.Dominio;

internal enum StatusPedido
{
    Created,
    Confirmed,
    Cancelled,
}

/// <summary>Transição de status que a regra do pedido não permite.</summary>
internal sealed class TransicaoInvalidaException(string mensagem) : InvalidOperationException(mensagem);

/// <summary>
/// Item do pedido. Guarda uma CÓPIA do nome e do preço do produto no momento da compra:
/// o pedido não aponta para a tabela do Catálogo (nem FK, nem navegação).
/// </summary>
internal sealed class ItemPedido
{
    public ItemPedido(Guid produtoId, string nomeProduto, decimal precoUnitario, int quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        ProdutoId = produtoId;
        NomeProduto = nomeProduto;
        PrecoUnitario = precoUnitario;
        Quantidade = quantidade;
    }

    public Guid ProdutoId { get; private set; }
    public string NomeProduto { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public int Quantidade { get; private set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;
}

/// <summary>Pedido: agregado PRIVADO do módulo Pedidos.</summary>
internal sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    private Pedido() { } // EF Core

    public Pedido(Guid id, Guid clienteId, DateTimeOffset criadoEm)
    {
        Id = id;
        ClienteId = clienteId;
        CriadoEm = criadoEm;
        Status = StatusPedido.Created;
    }

    public Guid Id { get; private set; }

    /// <summary>Só o Id do cliente: o cliente é do módulo Clientes (sem FK entre schemas).</summary>
    public Guid ClienteId { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }
    public StatusPedido Status { get; private set; }
    public IReadOnlyList<ItemPedido> Itens => _itens;
    public decimal Total => _itens.Sum(i => i.Subtotal);

    public void AdicionarItem(Guid produtoId, string nomeProduto, decimal precoUnitario, int quantidade)
    {
        if (Status != StatusPedido.Created)
            throw new TransicaoInvalidaException($"Pedido {Id} está {Status}: não aceita novos itens.");
        _itens.Add(new ItemPedido(produtoId, nomeProduto, precoUnitario, quantidade));
    }

    public void Confirmar()
    {
        if (Status != StatusPedido.Created)
            throw new TransicaoInvalidaException($"Pedido {Id} está {Status}: só pedidos Created podem ser confirmados.");
        if (_itens.Count == 0)
            throw new TransicaoInvalidaException($"Pedido {Id} não tem itens.");
        Status = StatusPedido.Confirmed;
    }

    public void Cancelar()
    {
        if (Status != StatusPedido.Created)
            throw new TransicaoInvalidaException($"Pedido {Id} está {Status}: só pedidos Created podem ser cancelados.");
        Status = StatusPedido.Cancelled;
    }
}
