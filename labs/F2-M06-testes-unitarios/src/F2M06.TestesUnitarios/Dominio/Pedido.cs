namespace F2M06.TestesUnitarios.Dominio;

/// <summary>Estados do pedido: Created → Confirmed → Completed; Created → Cancelled.</summary>
public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}

/// <summary>Violação de regra de negócio (mensagem pensada para o usuário).</summary>
public sealed class RegraDeNegocioException(string mensagem) : Exception(mensagem);

/// <summary>Linha do pedido. O preço é copiado do produto no momento da criação (não muda se o catálogo mudar).</summary>
public sealed record ItemPedido(Guid ProdutoId, string NomeProduto, decimal PrecoUnitario, int Quantidade)
{
    public decimal Subtotal => PrecoUnitario * Quantidade;
}

/// <summary>
/// Pedido do OrderFlow. Regras:
/// ao menos um item; quantidade &gt; 0; produto inativo não entra; o mesmo produto repetido vira uma linha só;
/// total calculado aqui (nunca vem do cliente); prazo de pagamento de 30 minutos a partir da criação.
/// </summary>
public sealed class Pedido
{
    /// <summary>Tempo que o cliente tem para pagar antes de o pedido expirar.</summary>
    public static readonly TimeSpan PrazoDePagamento = TimeSpan.FromMinutes(30);

    private Pedido(Guid id, Guid clienteId, DateTimeOffset criadoEm, List<ItemPedido> itens)
    {
        Id = id;
        ClienteId = clienteId;
        CriadoEm = criadoEm;
        Itens = itens.AsReadOnly();
    }

    public Guid Id { get; }
    public Guid ClienteId { get; }
    public DateTimeOffset CriadoEm { get; }
    public IReadOnlyList<ItemPedido> Itens { get; }
    public StatusPedido Status { get; private set; } = StatusPedido.Created;

    public decimal Total => Itens.Sum(i => i.Subtotal);
    public DateTimeOffset ExpiraEm => CriadoEm + PrazoDePagamento;

    /// <summary>Pedido "em aberto" conta para o limite por cliente: ainda não foi concluído nem cancelado.</summary>
    public bool EmAberto => Status is StatusPedido.Created or StatusPedido.Confirmed;

    /// <summary>Cria um pedido validando todas as regras.</summary>
    /// <exception cref="RegraDeNegocioException">Sem itens, quantidade inválida ou produto inativo.</exception>
    public static Pedido Criar(Guid clienteId, IEnumerable<(Produto Produto, int Quantidade)> itens, DateTimeOffset agora)
    {
        if (clienteId == Guid.Empty)
            throw new ArgumentException("Cliente é obrigatório.", nameof(clienteId));
        ArgumentNullException.ThrowIfNull(itens);

        var linhas = itens.ToList();
        if (linhas.Count == 0)
            throw new RegraDeNegocioException("Pedido precisa de ao menos um item.");

        foreach (var (produto, quantidade) in linhas)
        {
            if (quantidade <= 0)
                throw new RegraDeNegocioException($"Quantidade do produto '{produto.Nome}' deve ser maior que zero.");
            if (!produto.Ativo)
                throw new RegraDeNegocioException($"Produto '{produto.Nome}' está inativo.");
        }

        var agrupados = linhas
            .GroupBy(l => l.Produto.Id)
            .Select(g => new ItemPedido(g.Key, g.First().Produto.Nome, g.First().Produto.Preco, g.Sum(l => l.Quantidade)))
            .ToList();

        return new Pedido(Guid.NewGuid(), clienteId, agora, agrupados);
    }

    /// <summary>Expirado = ainda Created e o prazo de pagamento passou.</summary>
    public bool EstaExpirado(DateTimeOffset agora) => Status == StatusPedido.Created && agora >= ExpiraEm;

    public void Confirmar()
    {
        if (Status != StatusPedido.Created)
            throw new RegraDeNegocioException($"Só pedido Created pode ser confirmado (status atual: {Status}).");
        Status = StatusPedido.Confirmed;
    }

    public void Cancelar()
    {
        if (Status != StatusPedido.Created)
            throw new RegraDeNegocioException($"Só pedido Created pode ser cancelado (status atual: {Status}).");
        Status = StatusPedido.Cancelled;
    }
}
