using F2M08.Domain.Entidades;

namespace F2M08.Domain.Tests;

/// <summary>
/// Suíte "de produção" herdada: passa, cobre quase todas as linhas... e mesmo assim deixa
/// muitos mutantes vivos. Repare nos asserts fracos (ShouldBeGreaterThan(0), Should.NotThrow,
/// Throw&lt;Exception&gt;, valores longe dos limites). NÃO apague estes testes: na Parte 2 você
/// ACRESCENTA testes até o Stryker chegar ao mutation score alvo.
/// </summary>
public sealed class PedidoTests
{
    private static Produto Teclado() => new(Guid.NewGuid(), "Teclado", 200m);

    private static Pedido NovoPedido() => new(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void NovoPedido_ComecaComoCreated()
    {
        NovoPedido().Status.ShouldBe(StatusPedido.Created);
    }

    [Fact]
    public void AdicionarItem_ProdutoAtivo_TotalFicaPositivo()
    {
        var pedido = NovoPedido();

        pedido.AdicionarItem(Teclado(), 2);

        pedido.Total.ShouldBeGreaterThan(0m);
        pedido.Itens.ShouldNotBeEmpty();
    }

    [Fact]
    public void AdicionarItem_QuantidadeNegativa_Lanca()
    {
        var pedido = NovoPedido();

        Should.Throw<Exception>(() => pedido.AdicionarItem(Teclado(), -5));
    }

    [Fact]
    public void Confirmar_ComItens_NaoLanca()
    {
        var pedido = NovoPedido();
        pedido.AdicionarItem(Teclado(), 1);

        Should.NotThrow(pedido.Confirmar);
    }

    [Fact]
    public void FluxoCompleto_ConfirmarEConcluir_NaoLanca()
    {
        var pedido = NovoPedido();
        pedido.AdicionarItem(Teclado(), 1);

        Should.NotThrow(() =>
        {
            pedido.Confirmar();
            pedido.Concluir();
        });
    }

    [Fact]
    public void Cancelar_PedidoCreated_FicaCancelled()
    {
        var pedido = NovoPedido();

        pedido.Cancelar();

        pedido.Status.ShouldBe(StatusPedido.Cancelled);
    }
}
