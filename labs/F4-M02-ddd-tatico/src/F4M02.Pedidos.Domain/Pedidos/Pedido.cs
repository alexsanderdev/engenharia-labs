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

    // PRONTO: construtor privado — ninguém de fora faz "new Pedido(...)". O único jeito de nascer é Criar(...).
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
    public IReadOnlyList<ItemDoPedido> Itens =>
        throw new NotImplementedException($"TODO (Passo 2): exponha os {_itens.Count} itens como SOMENTE LEITURA (AsReadOnly) — nunca a List.");

    /// <summary>Soma dos subtotais dos itens (zero na moeda do pedido se não houver itens).</summary>
    public Dinheiro Subtotal => throw new NotImplementedException("TODO (Passo 2): some os subtotais partindo de Dinheiro.Zero(Moeda).");

    /// <summary>Desconto aplicado (zero por padrão). Só a <see cref="Descontos.PoliticaDeDesconto"/> (ou outro serviço) decide quanto.</summary>
    public Dinheiro Desconto { get; private set; }

    /// <summary>Subtotal − desconto. Nunca informado por quem chama.</summary>
    public Dinheiro Total => throw new NotImplementedException("TODO (Passo 2): Subtotal - Desconto.");

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
    public static Pedido Criar(ClienteId clienteId, EnderecoDeEntrega enderecoDeEntrega, string moeda, DateTimeOffset agora) =>
        throw new NotImplementedException("TODO (Passo 2): rejeite clienteId default; valide/normalize a moeda via Dinheiro.Zero(moeda).Moeda; crie com PedidoId.Novo(); Registrar(new PedidoCriado(...)).");

    /// <summary>Coloca um produto no pedido, copiando nome, SKU e preço do catálogo. Remove o desconto aplicado.</summary>
    /// <exception cref="RegraDeNegocioVioladaException">
    /// <see cref="Regras.PedidoNaoEditavel"/>, <see cref="Regras.ProdutoInativo"/>, <see cref="Regras.MoedasDiferentes"/>,
    /// <see cref="Regras.ProdutoRepetido"/> ou <see cref="Regras.LimiteDeItens"/>.
    /// </exception>
    public void AdicionarItem(ProdutoDoCatalogo produto, Quantidade quantidade) =>
        throw new NotImplementedException("TODO (Passo 2/3): editável? ativo? mesma moeda? já está no pedido? cabe no limite? → adicione o item e remova o desconto.");

    /// <summary>Troca a quantidade de um item existente. Remove o desconto aplicado.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.PedidoNaoEditavel"/> ou <see cref="Regras.ItemNaoEncontrado"/>.</exception>
    public void AlterarQuantidade(ProdutoId produtoId, Quantidade novaQuantidade) =>
        throw new NotImplementedException("TODO (Passo 3): editável? ache o item (ou Regras.ItemNaoEncontrado), altere a quantidade NO MESMO item e remova o desconto.");

    /// <summary>Tira um item do pedido. Remove o desconto aplicado.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.PedidoNaoEditavel"/> ou <see cref="Regras.ItemNaoEncontrado"/>.</exception>
    public void RemoverItem(ProdutoId produtoId) =>
        throw new NotImplementedException("TODO (Passo 3): editável? ache o item (ou Regras.ItemNaoEncontrado), remova e remova o desconto.");

    /// <summary>
    /// Aplica (substitui) o desconto do pedido. Quem calcula o valor é um domain service
    /// (<see cref="Descontos.PoliticaDeDesconto"/>); o agregado só garante que o valor é coerente.
    /// </summary>
    /// <exception cref="RegraDeNegocioVioladaException">
    /// <see cref="Regras.PedidoNaoEditavel"/>, <see cref="Regras.MoedasDiferentes"/> ou
    /// <see cref="Regras.DescontoInvalido"/> (desconto maior que o subtotal).
    /// </exception>
    public void AplicarDesconto(Dinheiro desconto) =>
        throw new NotImplementedException("TODO (Passo 6): editável? desconto > Subtotal → Regras.DescontoInvalido (a comparação já barra outra moeda). Guarde em Desconto.");

    /// <summary>Created → Confirmed. Exige ao menos um item. Registra <see cref="PedidoConfirmado"/> com o total.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.TransicaoInvalida"/> ou <see cref="Regras.PedidoSemItens"/>.</exception>
    public void Confirmar(DateTimeOffset agora) =>
        throw new NotImplementedException("TODO (Passo 4): transição permitida? tem item? → Status = Confirmed e Registrar(new PedidoConfirmado(...)).");

    /// <summary>Confirmed → Completed.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.TransicaoInvalida"/>.</exception>
    public void Concluir() =>
        throw new NotImplementedException("TODO (Passo 4): só a partir de Confirmed.");

    /// <summary>Created → Cancelled, com motivo (sem espaços nas pontas). Registra <see cref="PedidoCancelado"/>.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.MotivoObrigatorio"/> ou <see cref="Regras.TransicaoInvalida"/>.</exception>
    public void Cancelar(string motivo, DateTimeOffset agora) =>
        throw new NotImplementedException("TODO (Passo 4): motivo obrigatório; só a partir de Created (Completed não cancela); Registrar(new PedidoCancelado(...)).");
}
