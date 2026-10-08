using System.Collections.Concurrent;

namespace F5M04.Api.Pedidos;

public enum StatusPedido { Created, Confirmed, Completed, Cancelled }

public sealed record ItemPedido(Guid ProdutoId, string Nome, decimal PrecoUnitario, int Quantidade)
{
    public decimal Subtotal => PrecoUnitario * Quantidade;
}

/// <summary>
/// Pedido do OrderFlow. O <see cref="ClienteId"/> é o DONO do recurso: é ele que a autorização
/// baseada em recurso compara com o <c>sub</c> do token.
/// </summary>
public sealed class Pedido
{
    private Pedido(Guid id, Guid clienteId, IReadOnlyList<ItemPedido> itens, DateTimeOffset criadoEm)
    {
        Id = id;
        ClienteId = clienteId;
        Itens = itens;
        CriadoEm = criadoEm;
    }

    public Guid Id { get; }
    public Guid ClienteId { get; }
    public IReadOnlyList<ItemPedido> Itens { get; }
    public DateTimeOffset CriadoEm { get; }
    public StatusPedido Status { get; private set; } = StatusPedido.Created;
    public decimal Total => Itens.Sum(i => i.Subtotal);

    public static Pedido Criar(Guid clienteId, IReadOnlyList<ItemPedido> itens, DateTimeOffset agora)
    {
        if (clienteId == Guid.Empty) throw new ArgumentException("Cliente obrigatório.", nameof(clienteId));
        if (itens.Count == 0) throw new ArgumentException("O pedido precisa de ao menos um item.", nameof(itens));
        return new Pedido(Guid.NewGuid(), clienteId, itens, agora);
    }

    /// <summary>Created → Cancelled. Devolve <c>false</c> se o status atual não permite cancelar.</summary>
    public bool Cancelar()
    {
        if (Status is not StatusPedido.Created) return false;
        Status = StatusPedido.Cancelled;
        return true;
    }
}

public interface IPedidoRepositorio
{
    void Adicionar(Pedido pedido);
    Pedido? Obter(Guid id);
    IReadOnlyList<Pedido> ListarDoCliente(Guid clienteId);
}

public sealed class PedidoRepositorioEmMemoria : IPedidoRepositorio
{
    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();

    public void Adicionar(Pedido pedido) => _pedidos[pedido.Id] = pedido;

    public Pedido? Obter(Guid id) => _pedidos.GetValueOrDefault(id);

    public IReadOnlyList<Pedido> ListarDoCliente(Guid clienteId) =>
        [.. _pedidos.Values.Where(p => p.ClienteId == clienteId).OrderBy(p => p.CriadoEm)];
}
