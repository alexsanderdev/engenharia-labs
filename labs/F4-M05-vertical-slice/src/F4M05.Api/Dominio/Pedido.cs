using F4M05.Api.Comum;

namespace F4M05.Api.Dominio;

/// <summary>Created → Confirmed → Completed; Created → Cancelled. Completed não cancela.</summary>
public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}

/// <summary>Produto do catálogo (preço oficial).</summary>
public sealed class Produto(Guid id, string nome, decimal preco, bool ativo)
{
    public Guid Id { get; } = id;
    public string Nome { get; } = nome;
    public decimal Preco { get; } = preco;
    public bool Ativo { get; } = ativo;
}

/// <summary>Linha do pedido com preço congelado.</summary>
public sealed class ItemPedido(Guid produtoId, string nome, int quantidade, decimal precoUnitario)
{
    public Guid ProdutoId { get; } = produtoId;
    public string Nome { get; } = nome;
    public int Quantidade { get; } = quantidade;
    public decimal PrecoUnitario { get; } = precoUnitario;
    public decimal Subtotal => PrecoUnitario * Quantidade;
}

/// <summary>
/// Agregado Pedido. As regras de transição moram AQUI, uma única vez — e não copiadas em cada fatia.
/// Fatias diferentes (Confirmar, Cancelar) chamam o mesmo domínio.
/// </summary>
public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    private Pedido(Guid id, Guid clienteId)
    {
        Id = id;
        ClienteId = clienteId;
        Status = StatusPedido.Created;
    }

    public Guid Id { get; }
    public Guid ClienteId { get; }
    public StatusPedido Status { get; private set; }
    public IReadOnlyList<ItemPedido> Itens => _itens;
    public decimal Total => _itens.Sum(i => i.Subtotal);

    public static Pedido Criar(Guid clienteId, IEnumerable<(Produto Produto, int Quantidade)> linhas)
    {
        var pedido = new Pedido(Guid.NewGuid(), clienteId);
        foreach (var (produto, quantidade) in linhas)
        {
            if (quantidade <= 0)
                throw new RegraDeNegocioException($"A quantidade de {produto.Nome} deve ser maior que zero.");
            if (!produto.Ativo)
                throw new RegraDeNegocioException($"O produto {produto.Nome} está inativo e não pode entrar em pedido.");
            pedido._itens.Add(new ItemPedido(produto.Id, produto.Nome, quantidade, produto.Preco));
        }

        if (pedido._itens.Count == 0)
            throw new RegraDeNegocioException("O pedido precisa ter pelo menos um item.");
        return pedido;
    }

    public void Confirmar() => Transicionar(StatusPedido.Confirmed);

    public void Cancelar()
    {
        if (Status == StatusPedido.Completed)
            throw new TransicaoInvalidaException("Pedido concluído não pode ser cancelado.");
        Transicionar(StatusPedido.Cancelled);
    }

    private void Transicionar(StatusPedido para)
    {
        if (Status != StatusPedido.Created)
            throw new TransicaoInvalidaException($"Transição inválida: {Status} → {para}.");
        Status = para;
    }
}
