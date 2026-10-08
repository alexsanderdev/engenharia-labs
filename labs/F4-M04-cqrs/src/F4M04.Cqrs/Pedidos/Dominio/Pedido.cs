using F4M04.Cqrs.Abstractions;

namespace F4M04.Cqrs.Pedidos.Dominio;

public enum StatusPedido { Created, Confirmed, Completed, Cancelled }

public sealed record ItemPedido(Guid ProdutoId, int Quantidade, decimal PrecoUnitario)
{
    public decimal Subtotal => Quantidade * PrecoUnitario;
}

public sealed record Produto(Guid Id, string Nome, decimal Preco, bool Ativo);

/// <summary>Violação de regra de negócio (ex.: produto inativo, transição de status inválida).</summary>
public sealed class RegraDeNegocioException(string message) : Exception(message);

/// <summary>O pedido referenciado pelo command não existe no lado de escrita.</summary>
public sealed class PedidoNaoEncontradoException(Guid pedidoId)
    : Exception($"Pedido {pedidoId} não encontrado.")
{
    public Guid PedidoId { get; } = pedidoId;
}

/// <summary>Evento: um pedido foi criado. Carrega o que o read model precisa.</summary>
public sealed record PedidoCriado(
    Guid PedidoId, Guid ClienteId, decimal Total, int QuantidadeItens, DateTimeOffset OcorreuEm) : IDomainEvent;

/// <summary>Evento: um pedido foi confirmado.</summary>
public sealed record PedidoConfirmado(Guid PedidoId, DateTimeOffset OcorreuEm) : IDomainEvent;

/// <summary>
/// Agregado do lado de ESCRITA. Protege as invariantes e registra eventos de domínio;
/// quem os publica é a unidade de trabalho, depois do commit. Nenhuma tela lê esta classe.
/// </summary>
public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];
    private readonly List<IDomainEvent> _eventos = [];

    private Pedido(Guid id, Guid clienteId, DateTimeOffset criadoEm)
    {
        Id = id;
        ClienteId = clienteId;
        CriadoEm = criadoEm;
        Status = StatusPedido.Created;
    }

    public Guid Id { get; }
    public Guid ClienteId { get; }
    public DateTimeOffset CriadoEm { get; }
    public DateTimeOffset? ConfirmadoEm { get; private set; }
    public StatusPedido Status { get; private set; }
    public IReadOnlyList<ItemPedido> Itens => _itens;
    public decimal Total => _itens.Sum(i => i.Subtotal);

    /// <summary>Eventos registrados e ainda não publicados.</summary>
    public IReadOnlyList<IDomainEvent> Eventos => _eventos;

    /// <summary>Cria o pedido com os itens (preços vindos do catálogo, nunca do cliente).</summary>
    public static Pedido Criar(Guid clienteId, IEnumerable<(Produto Produto, int Quantidade)> itens, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(itens);
        var pedido = new Pedido(Guid.CreateVersion7(agora), clienteId, agora);
        foreach (var (produto, quantidade) in itens)
        {
            if (!produto.Ativo)
                throw new RegraDeNegocioException($"O produto '{produto.Nome}' está inativo e não pode entrar em pedido.");
            if (quantidade <= 0)
                throw new RegraDeNegocioException("A quantidade deve ser maior que zero.");
            pedido._itens.Add(new ItemPedido(produto.Id, quantidade, produto.Preco));
        }

        if (pedido._itens.Count == 0)
            throw new RegraDeNegocioException("Um pedido precisa de pelo menos um item.");

        pedido._eventos.Add(new PedidoCriado(pedido.Id, clienteId, pedido.Total, pedido._itens.Sum(i => i.Quantidade), agora));
        return pedido;
    }

    /// <summary>Created → Confirmed. Qualquer outra origem é violação de regra.</summary>
    public void Confirmar(DateTimeOffset agora)
    {
        if (Status != StatusPedido.Created)
            throw new RegraDeNegocioException($"Só pedidos Created podem ser confirmados (status atual: {Status}).");

        Status = StatusPedido.Confirmed;
        ConfirmadoEm = agora;
        _eventos.Add(new PedidoConfirmado(Id, agora));
    }

    /// <summary>Entrega os eventos pendentes e limpa a lista (chamado pela unidade de trabalho).</summary>
    public IReadOnlyList<IDomainEvent> RetirarEventos()
    {
        var eventos = _eventos.ToArray();
        _eventos.Clear();
        return eventos;
    }
}
