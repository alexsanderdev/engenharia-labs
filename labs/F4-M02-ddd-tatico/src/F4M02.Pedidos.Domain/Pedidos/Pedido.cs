using F4M02.Pedidos.Domain.Comum;
using F4M02.Pedidos.Domain.ValueObjects;

namespace F4M02.Pedidos.Domain.Pedidos;

/// <summary>
/// Raiz do agregado Pedido. Tudo que muda o pedido passa por aqui, e nenhuma sequência de chamadas
/// públicas deixa o pedido num estado inválido.
/// </summary>
/// <remarks>
/// Invariantes protegidas:
/// <list type="bullet">
/// <item>Itens só mudam enquanto o pedido está em <see cref="StatusPedido.Created"/>.</item>
/// <item>Produto inativo não entra; preço tem de estar na moeda do pedido; um produto aparece uma vez só; no máximo <see cref="MaximoDeItens"/> itens.</item>
/// <item>Subtotal e total são sempre calculados; o desconto nunca passa do subtotal e é removido quando os itens mudam.</item>
/// <item>Transições: Created → Confirmed → Completed; Created → Cancelled. Confirmar exige ao menos 1 item; cancelar exige motivo.</item>
/// </list>
/// Referências a outros agregados (cliente, produto) são feitas por id.
/// </remarks>
public sealed class Pedido : RaizDeAgregado<PedidoId>
{
    /// <summary>Quantidade máxima de itens (produtos distintos) por pedido.</summary>
    public const int MaximoDeItens = 20;

    private readonly List<ItemDoPedido> _itens = [];

    private Pedido(PedidoId id, ClienteId clienteId, EnderecoDeEntrega enderecoDeEntrega, string moeda, DateTimeOffset criadoEm)
        : base(id)
    {
        ClienteId = clienteId;
        EnderecoDeEntrega = enderecoDeEntrega;
        Moeda = moeda;
        CriadoEm = criadoEm;
        Status = StatusPedido.Created;
        Desconto = Dinheiro.Zero(moeda);
    }

    /// <summary>Cliente dono do pedido (referência por id a outro agregado/contexto).</summary>
    public ClienteId ClienteId { get; }

    public EnderecoDeEntrega EnderecoDeEntrega { get; }

    /// <summary>Moeda do pedido (ISO 4217). Todos os preços e o desconto precisam estar nela.</summary>
    public string Moeda { get; }

    public DateTimeOffset CriadoEm { get; }

    public StatusPedido Status { get; private set; }

    /// <summary>Itens do pedido, somente leitura para quem está fora do agregado.</summary>
    public IReadOnlyList<ItemDoPedido> Itens => _itens.AsReadOnly();

    /// <summary>Soma dos subtotais dos itens (zero na moeda do pedido se não houver itens).</summary>
    public Dinheiro Subtotal => _itens.Aggregate(Dinheiro.Zero(Moeda), (soma, item) => soma + item.Subtotal);

    /// <summary>Desconto aplicado (zero por padrão). Só a <see cref="Descontos.PoliticaDeDesconto"/> (ou outro serviço) decide quanto.</summary>
    public Dinheiro Desconto { get; private set; }

    /// <summary>Subtotal − desconto. Nunca informado por quem chama.</summary>
    public Dinheiro Total => Subtotal - Desconto;

    /// <summary>
    /// Factory method: abre um pedido em <see cref="StatusPedido.Created"/>, sem itens, com id novo,
    /// e registra <see cref="PedidoCriado"/>.
    /// </summary>
    /// <param name="clienteId">Cliente dono do pedido (não pode ser o <c>default</c>).</param>
    /// <param name="enderecoDeEntrega">Endereço já validado (é um value object).</param>
    /// <param name="moeda">Moeda do pedido (ISO 4217; é normalizada).</param>
    /// <param name="agora">Instante atual — quem chama lê do <see cref="TimeProvider"/>.</param>
    /// <exception cref="ArgumentException">Se <paramref name="clienteId"/> for o <c>default</c>.</exception>
    /// <exception cref="RegraDeNegocioVioladaException">Se a moeda for inválida.</exception>
    public static Pedido Criar(ClienteId clienteId, EnderecoDeEntrega enderecoDeEntrega, string moeda, DateTimeOffset agora)
    {
        if (clienteId == default)
            throw new ArgumentException("ClienteId é obrigatório.", nameof(clienteId));
        ArgumentNullException.ThrowIfNull(enderecoDeEntrega);
        var moedaValidada = Dinheiro.Zero(moeda).Moeda;

        var pedido = new Pedido(PedidoId.Novo(), clienteId, enderecoDeEntrega, moedaValidada, agora);
        pedido.Registrar(new PedidoCriado(pedido.Id, clienteId, agora));
        return pedido;
    }

    /// <summary>Coloca um produto no pedido, copiando nome, SKU e preço do catálogo. Remove o desconto aplicado.</summary>
    /// <exception cref="RegraDeNegocioVioladaException">
    /// <see cref="Regras.PedidoNaoEditavel"/>, <see cref="Regras.ProdutoInativo"/>, <see cref="Regras.MoedasDiferentes"/>,
    /// <see cref="Regras.ProdutoRepetido"/> ou <see cref="Regras.LimiteDeItens"/>.
    /// </exception>
    public void AdicionarItem(ProdutoDoCatalogo produto, Quantidade quantidade)
    {
        ArgumentNullException.ThrowIfNull(produto);
        ArgumentNullException.ThrowIfNull(quantidade);
        GarantirEditavel();

        if (!produto.Ativo)
            throw new RegraDeNegocioVioladaException(Regras.ProdutoInativo, $"O produto {produto.Sku} está inativo e não pode entrar no pedido.");
        if (produto.Preco.Moeda != Moeda)
            throw new RegraDeNegocioVioladaException(Regras.MoedasDiferentes, $"O pedido é em {Moeda}, mas o preço do produto {produto.Sku} está em {produto.Preco.Moeda}.");
        if (_itens.Exists(i => i.ProdutoId == produto.Id))
            throw new RegraDeNegocioVioladaException(Regras.ProdutoRepetido, $"O produto {produto.Sku} já está no pedido: altere a quantidade do item.");
        if (_itens.Count >= MaximoDeItens)
            throw new RegraDeNegocioVioladaException(Regras.LimiteDeItens, $"Um pedido tem no máximo {MaximoDeItens} itens.");

        _itens.Add(new ItemDoPedido(produto, quantidade));
        RemoverDesconto();
    }

    /// <summary>Troca a quantidade de um item existente. Remove o desconto aplicado.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.PedidoNaoEditavel"/> ou <see cref="Regras.ItemNaoEncontrado"/>.</exception>
    public void AlterarQuantidade(ProdutoId produtoId, Quantidade novaQuantidade)
    {
        ArgumentNullException.ThrowIfNull(novaQuantidade);
        GarantirEditavel();
        ItemDo(produtoId).AlterarQuantidade(novaQuantidade);
        RemoverDesconto();
    }

    /// <summary>Tira um item do pedido. Remove o desconto aplicado.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.PedidoNaoEditavel"/> ou <see cref="Regras.ItemNaoEncontrado"/>.</exception>
    public void RemoverItem(ProdutoId produtoId)
    {
        GarantirEditavel();
        _itens.Remove(ItemDo(produtoId));
        RemoverDesconto();
    }

    /// <summary>
    /// Aplica (substitui) o desconto do pedido. Quem calcula o valor é um domain service
    /// (<see cref="Descontos.PoliticaDeDesconto"/>); o agregado só garante que o valor é coerente.
    /// </summary>
    /// <exception cref="RegraDeNegocioVioladaException">
    /// <see cref="Regras.PedidoNaoEditavel"/>, <see cref="Regras.MoedasDiferentes"/> ou
    /// <see cref="Regras.DescontoInvalido"/> (desconto maior que o subtotal).
    /// </exception>
    public void AplicarDesconto(Dinheiro desconto)
    {
        ArgumentNullException.ThrowIfNull(desconto);
        GarantirEditavel();
        if (desconto > Subtotal)
            throw new RegraDeNegocioVioladaException(Regras.DescontoInvalido, $"Desconto de {desconto} maior que o subtotal de {Subtotal}.");

        Desconto = desconto;
    }

    /// <summary>Created → Confirmed. Exige ao menos um item. Registra <see cref="PedidoConfirmado"/> com o total.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.TransicaoInvalida"/> ou <see cref="Regras.PedidoSemItens"/>.</exception>
    public void Confirmar(DateTimeOffset agora)
    {
        GarantirTransicao(StatusPedido.Confirmed);
        if (_itens.Count == 0)
            throw new RegraDeNegocioVioladaException(Regras.PedidoSemItens, "Pedido sem itens não pode ser confirmado.");

        Status = StatusPedido.Confirmed;
        Registrar(new PedidoConfirmado(Id, ClienteId, Total, _itens.Count, agora));
    }

    /// <summary>Confirmed → Completed.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.TransicaoInvalida"/>.</exception>
    public void Concluir()
    {
        GarantirTransicao(StatusPedido.Completed);
        Status = StatusPedido.Completed;
    }

    /// <summary>Created → Cancelled, com motivo (sem espaços nas pontas). Registra <see cref="PedidoCancelado"/>.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.MotivoObrigatorio"/> ou <see cref="Regras.TransicaoInvalida"/>.</exception>
    public void Cancelar(string motivo, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new RegraDeNegocioVioladaException(Regras.MotivoObrigatorio, "Informe o motivo do cancelamento.");
        GarantirTransicao(StatusPedido.Cancelled);

        Status = StatusPedido.Cancelled;
        Registrar(new PedidoCancelado(Id, ClienteId, motivo.Trim(), agora));
    }

    private static bool TransicaoPermitida(StatusPedido de, StatusPedido para) => (de, para) switch
    {
        (StatusPedido.Created, StatusPedido.Confirmed) => true,
        (StatusPedido.Confirmed, StatusPedido.Completed) => true,
        (StatusPedido.Created, StatusPedido.Cancelled) => true,
        _ => false,
    };

    private void GarantirTransicao(StatusPedido para)
    {
        if (!TransicaoPermitida(Status, para))
            throw new RegraDeNegocioVioladaException(Regras.TransicaoInvalida, $"Transição inválida: {Status} → {para}.");
    }

    private void GarantirEditavel()
    {
        if (Status != StatusPedido.Created)
            throw new RegraDeNegocioVioladaException(Regras.PedidoNaoEditavel, $"Pedido em {Status} não pode ser alterado.");
    }

    private ItemDoPedido ItemDo(ProdutoId produtoId) =>
        _itens.Find(i => i.ProdutoId == produtoId)
        ?? throw new RegraDeNegocioVioladaException(Regras.ItemNaoEncontrado, $"O produto {produtoId} não está no pedido.");

    private void RemoverDesconto() => Desconto = Dinheiro.Zero(Moeda);
}
