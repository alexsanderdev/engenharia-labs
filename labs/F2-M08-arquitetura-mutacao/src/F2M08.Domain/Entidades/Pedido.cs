using F2M08.Domain.Descontos;

namespace F2M08.Domain.Entidades;

/// <summary>
/// Agregado Pedido. Regras: quantidade &gt; 0, produto inativo não entra, itens só mudam em Created,
/// pedido vazio não confirma, Created → Confirmed → Completed, Created → Cancelled, Completed não cancela.
/// Subtotal, desconto e total são SEMPRE calculados aqui.
/// </summary>
public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    public Pedido(Guid id, Guid clienteId)
    {
        Id = id;
        ClienteId = clienteId;
        Status = StatusPedido.Created;
    }

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public StatusPedido Status { get; private set; }
    public IReadOnlyList<ItemPedido> Itens => _itens;

    public int Unidades => _itens.Sum(i => i.Quantidade);
    public decimal Subtotal => _itens.Sum(i => i.Subtotal);
    public decimal Desconto => PoliticaDeDesconto.CalcularDesconto(Subtotal, Unidades);
    public decimal Total => Subtotal - Desconto;

    public void AdicionarItem(Produto produto, int quantidade)
    {
        ArgumentNullException.ThrowIfNull(produto);
        if (Status != StatusPedido.Created)
            throw new InvalidOperationException("Só é possível alterar itens de um pedido Created.");
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade), quantidade, "A quantidade deve ser maior que zero.");
        if (!produto.Ativo)
            throw new InvalidOperationException($"O produto {produto.Nome} está inativo e não pode entrar em pedido.");

        var existente = _itens.Find(i => i.ProdutoId == produto.Id);
        if (existente is not null)
            existente.Somar(quantidade);
        else
            _itens.Add(new ItemPedido(produto.Id, quantidade, produto.Preco));
    }

    public void Confirmar()
    {
        if (_itens.Count == 0)
            throw new InvalidOperationException("Pedido sem itens não pode ser confirmado.");
        Transicionar(StatusPedido.Created, StatusPedido.Confirmed);
    }

    public void Concluir() => Transicionar(StatusPedido.Confirmed, StatusPedido.Completed);

    public void Cancelar()
    {
        if (Status == StatusPedido.Completed)
            throw new InvalidOperationException("Pedido concluído não pode ser cancelado.");
        Transicionar(StatusPedido.Created, StatusPedido.Cancelled);
    }

    private void Transicionar(StatusPedido de, StatusPedido para)
    {
        if (Status != de)
            throw new InvalidOperationException($"Transição inválida: {Status} → {para}.");
        Status = para;
    }
}
