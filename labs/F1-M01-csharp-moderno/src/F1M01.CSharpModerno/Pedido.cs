namespace F1M01.CSharpModerno;

/// <summary>Cliente do pedido (record posicional).</summary>
public sealed record Cliente(string Nome, bool Vip = false);

/// <summary>Item do pedido (record posicional com propriedade calculada).</summary>
public sealed record ItemPedido(Produto Produto, int Quantidade)
{
    /// <summary>Preço do produto × quantidade.</summary>
    public Dinheiro Subtotal => Produto.Preco * Quantidade;
}

/// <summary>
/// Pedido: entidade com identidade e estado mutável controlado. Usa primary constructor.
/// </summary>
public sealed class Pedido(Cliente cliente)
{
    private readonly List<ItemPedido> _itens = [];

    /// <summary>Cliente obrigatório (nulo lança <see cref="ArgumentNullException"/>).</summary>
    public Cliente Cliente { get; } = cliente ?? throw new ArgumentNullException(nameof(cliente));

    /// <summary>Status atual. Começa em <see cref="StatusPedido.Criado"/>.</summary>
    public StatusPedido Status { get; private set; } = StatusPedido.Criado;

    /// <summary>Itens somente leitura.</summary>
    public IReadOnlyList<ItemPedido> Itens => _itens;

    /// <summary>Soma dos subtotais dos itens (zero em reais se não houver itens).</summary>
    public Dinheiro Subtotal =>
        throw new NotImplementedException("TODO: some os subtotais dos itens partindo de Dinheiro.Zero()");

    /// <summary>
    /// Adiciona um item. Regras: quantidade &lt;= 0 lança <see cref="ArgumentOutOfRangeException"/>;
    /// produto inativo lança <see cref="InvalidOperationException"/>;
    /// pedido fora do status Criado lança <see cref="InvalidOperationException"/>.
    /// </summary>
    public void AdicionarItem(Produto produto, int quantidade)
    {
        // TODO: valide quantidade, status e produto ativo; depois adicione o item em _itens.
        throw new NotImplementedException("TODO: implemente Pedido.AdicionarItem");
    }

    /// <summary>
    /// Altera o status respeitando <see cref="TransicoesDePedido.PodeTransicionar"/>.
    /// Transição inválida lança <see cref="InvalidOperationException"/>.
    /// </summary>
    public void AlterarStatus(StatusPedido novo)
    {
        // TODO: use TransicoesDePedido.PodeTransicionar e atualize Status.
        throw new NotImplementedException("TODO: implemente Pedido.AlterarStatus");
    }
}
