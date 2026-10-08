namespace F4M02.Pedidos.Domain.Tests;

/// <summary>
/// Passos 4 e 5 — Transições de status e domain events acumulados na raiz.
/// </summary>
public sealed class CicloDeVidaDoPedidoTests
{
    private static readonly DateTimeOffset MaisTarde = Dado.Agora.AddMinutes(10);

    [Fact]
    public void Confirmar_ExigeAoMenosUmItem_EAnunciaPedidoConfirmadoComOTotal()
    {
        var vazio = Dado.PedidoNovo();
        Dado.RegraVioladaPor(() => vazio.Confirmar(MaisTarde)).ShouldBe(Regras.PedidoSemItens);
        vazio.Status.ShouldBe(StatusPedido.Created);
        vazio.EventosDeDominio.ShouldNotContain(e => e is PedidoConfirmado);

        var pedido = Dado.PedidoComItem(preco: 12.5m, quantidade: 2);

        pedido.Confirmar(MaisTarde);

        pedido.Status.ShouldBe(StatusPedido.Confirmed);
        pedido.EventosDeDominio[^1]
            .ShouldBe(new PedidoConfirmado(pedido.Id, Dado.Cliente, Dinheiro.Reais(25m), 1, MaisTarde));
    }

    [Fact]
    public void Concluir_PedidoConfirmado_FicaCompleted_EPedidoConcluidoNaoCancela()
    {
        var pedido = Dado.PedidoNoStatus(StatusPedido.Confirmed);

        pedido.Concluir();

        pedido.Status.ShouldBe(StatusPedido.Completed);
        var ex = Should.Throw<RegraDeNegocioVioladaException>(() => pedido.Cancelar("Arrependimento", MaisTarde));
        ex.Regra.ShouldBe(Regras.TransicaoInvalida);
        ex.Message.ShouldContain("Completed");
        ex.Message.ShouldContain("Cancelled");
        pedido.Status.ShouldBe(StatusPedido.Completed);
    }

    [Fact]
    public void Cancelar_PedidoCriado_ExigeMotivo_FicaCancelled_EAnunciaPedidoCancelado()
    {
        var pedido = Dado.PedidoComItem();

        Dado.RegraVioladaPor(() => pedido.Cancelar("   ", MaisTarde)).ShouldBe(Regras.MotivoObrigatorio);
        pedido.Status.ShouldBe(StatusPedido.Created);

        pedido.Cancelar("  Cliente desistiu  ", MaisTarde);

        pedido.Status.ShouldBe(StatusPedido.Cancelled);
        pedido.EventosDeDominio[^1]
            .ShouldBe(new PedidoCancelado(pedido.Id, Dado.Cliente, "Cliente desistiu", MaisTarde));
    }

    [Theory]
    [InlineData(StatusPedido.Created, "Concluir")]
    [InlineData(StatusPedido.Confirmed, "Confirmar")]
    [InlineData(StatusPedido.Confirmed, "Cancelar")]
    [InlineData(StatusPedido.Completed, "Confirmar")]
    [InlineData(StatusPedido.Completed, "Concluir")]
    [InlineData(StatusPedido.Cancelled, "Confirmar")]
    [InlineData(StatusPedido.Cancelled, "Concluir")]
    [InlineData(StatusPedido.Cancelled, "Cancelar")]
    public void TransicaoInvalida_EhRejeitada_SemMudarStatusNemRegistrarEvento(StatusPedido statusInicial, string acao)
    {
        var pedido = Dado.PedidoNoStatus(statusInicial);
        var eventosAntes = pedido.EventosDeDominio.Count;

        Dado.RegraVioladaPor(() => Executar(pedido, acao)).ShouldBe(Regras.TransicaoInvalida);

        pedido.Status.ShouldBe(statusInicial);
        pedido.EventosDeDominio.Count.ShouldBe(eventosAntes);
    }

    [Theory]
    [InlineData(StatusPedido.Confirmed)]
    [InlineData(StatusPedido.Completed)]
    [InlineData(StatusPedido.Cancelled)]
    public void PedidoForaDeCreated_NaoAceitaMudancaNosItensNemDesconto(StatusPedido status)
    {
        var pedido = Dado.PedidoNoStatus(status);
        var produtoNoPedido = pedido.Itens[0].ProdutoId;

        Dado.RegraVioladaPor(() => pedido.AdicionarItem(Dado.Produto("OUTRO-1"), new Quantidade(1))).ShouldBe(Regras.PedidoNaoEditavel);
        Dado.RegraVioladaPor(() => pedido.AlterarQuantidade(produtoNoPedido, new Quantidade(9))).ShouldBe(Regras.PedidoNaoEditavel);
        Dado.RegraVioladaPor(() => pedido.RemoverItem(produtoNoPedido)).ShouldBe(Regras.PedidoNaoEditavel);
        Dado.RegraVioladaPor(() => pedido.AplicarDesconto(Dinheiro.Reais(1m))).ShouldBe(Regras.PedidoNaoEditavel);

        pedido.Itens.ShouldHaveSingleItem().Quantidade.ShouldBe(new Quantidade(1));
    }

    [Fact]
    public void EventosDeDominio_AcumulamNaOrdemDosFatos_ELimparEventosEsvaziaALista()
    {
        var pedido = Dado.PedidoComItem();
        pedido.Confirmar(MaisTarde);

        pedido.EventosDeDominio.Select(e => e.GetType()).ShouldBe([typeof(PedidoCriado), typeof(PedidoConfirmado)]);
        pedido.EventosDeDominio.Select(e => e.OcorridoEm).ShouldBe([Dado.Agora, MaisTarde]);

        pedido.LimparEventos();

        pedido.EventosDeDominio.ShouldBeEmpty();
        pedido.Concluir();
        pedido.EventosDeDominio.ShouldBeEmpty(); // concluir não anuncia nada (ainda — ver desafios extras)
    }

    private static void Executar(Pedido pedido, string acao)
    {
        switch (acao)
        {
            case "Confirmar": pedido.Confirmar(MaisTarde); break;
            case "Concluir": pedido.Concluir(); break;
            case "Cancelar": pedido.Cancelar("Motivo qualquer", MaisTarde); break;
            default: throw new ArgumentOutOfRangeException(nameof(acao), acao, null);
        }
    }
}
