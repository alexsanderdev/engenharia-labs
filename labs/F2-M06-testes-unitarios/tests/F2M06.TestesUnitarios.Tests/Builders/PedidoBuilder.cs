using F2M06.TestesUnitarios.Dominio;

namespace F2M06.TestesUnitarios.Tests.Builders;

/// <summary>
/// Test Data Builder de <see cref="Pedido"/>. Passa SEMPRE pela fábrica <see cref="Pedido.Criar"/>
/// (nada de reflexão ou setters de teste): se a regra de domínio mudar, o builder acompanha.
/// Padrão: cliente aleatório, um item (produto padrão × 1), criado em <see cref="DataPadrao"/>, status Created.
/// </summary>
/// <remarks>
/// TODO (Passo 2): campos para cliente, data, lista de itens (nula = "usar o item padrão") e status desejado.
/// No Build: crie via Pedido.Criar e, se pedido Confirmado/Cancelado, chame Confirmar()/Cancelar().
/// </remarks>
public sealed class PedidoBuilder
{
    /// <summary>Data fixa e conhecida: testes determinísticos não usam "agora".</summary>
    public static readonly DateTimeOffset DataPadrao = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    public static PedidoBuilder UmPedido() => new();

    public PedidoBuilder DoCliente(Guid clienteId) =>
        throw new NotImplementedException("TODO: guarde o cliente e devolva this.");

    /// <summary>Adiciona um item. Ao chamar pela primeira vez, o item padrão deixa de existir.</summary>
    public PedidoBuilder ComItem(Produto produto, int quantidade = 1) =>
        throw new NotImplementedException("TODO: acumule (produto, quantidade); o item padrão some quando há itens explícitos.");

    /// <summary>Remove todos os itens (inclusive o padrão) — para testar a regra "ao menos um item".</summary>
    public PedidoBuilder SemItens() =>
        throw new NotImplementedException("TODO: lista de itens vazia (não nula).");

    public PedidoBuilder CriadoEm(DateTimeOffset criadoEm) =>
        throw new NotImplementedException("TODO: guarde a data e devolva this.");

    public PedidoBuilder Confirmado() =>
        throw new NotImplementedException("TODO: lembre que o Build deve confirmar o pedido.");

    public PedidoBuilder Cancelado() =>
        throw new NotImplementedException("TODO: lembre que o Build deve cancelar o pedido.");

    public Pedido Build() =>
        throw new NotImplementedException("TODO: Pedido.Criar(cliente, itens ?? [(UmProduto().Build(), 1)], data) + transição de status.");

    public static implicit operator Pedido(PedidoBuilder builder) => builder.Build();
}
