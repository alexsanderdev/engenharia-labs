using F2M06.TestesUnitarios.Dominio;

namespace F2M06.TestesUnitarios.Tests.Builders;

/// <summary>
/// Test Data Builder de <see cref="Pedido"/>. Passa SEMPRE pela fábrica <see cref="Pedido.Criar"/>
/// (nada de reflexão ou setters de teste): se a regra de domínio mudar, o builder acompanha.
/// Padrão: cliente aleatório, um item (produto padrão × 1), criado em <see cref="DataPadrao"/>, status Created.
/// </summary>
public sealed class PedidoBuilder
{
    /// <summary>Data fixa e conhecida: testes determinísticos não usam "agora".</summary>
    public static readonly DateTimeOffset DataPadrao = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private Guid _clienteId = Guid.NewGuid();
    private DateTimeOffset _criadoEm = DataPadrao;
    private List<(Produto Produto, int Quantidade)>? _itens;
    private StatusPedido _status = StatusPedido.Created;

    public static PedidoBuilder UmPedido() => new();

    public PedidoBuilder DoCliente(Guid clienteId)
    {
        _clienteId = clienteId;
        return this;
    }

    /// <summary>Adiciona um item. Ao chamar pela primeira vez, o item padrão deixa de existir.</summary>
    public PedidoBuilder ComItem(Produto produto, int quantidade = 1)
    {
        _itens ??= [];
        _itens.Add((produto, quantidade));
        return this;
    }

    /// <summary>Remove todos os itens (inclusive o padrão) — para testar a regra "ao menos um item".</summary>
    public PedidoBuilder SemItens()
    {
        _itens = [];
        return this;
    }

    public PedidoBuilder CriadoEm(DateTimeOffset criadoEm)
    {
        _criadoEm = criadoEm;
        return this;
    }

    public PedidoBuilder Confirmado()
    {
        _status = StatusPedido.Confirmed;
        return this;
    }

    public PedidoBuilder Cancelado()
    {
        _status = StatusPedido.Cancelled;
        return this;
    }

    public Pedido Build()
    {
        var itens = _itens ?? [(ProdutoBuilder.UmProduto().Build(), 1)];
        var pedido = Pedido.Criar(_clienteId, itens, _criadoEm);

        switch (_status)
        {
            case StatusPedido.Confirmed:
                pedido.Confirmar();
                break;
            case StatusPedido.Cancelled:
                pedido.Cancelar();
                break;
        }

        return pedido;
    }

    public static implicit operator Pedido(PedidoBuilder builder) => builder.Build();
}
