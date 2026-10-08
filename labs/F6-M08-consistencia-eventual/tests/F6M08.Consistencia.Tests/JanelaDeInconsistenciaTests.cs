using F6M08.Consistencia.Dominio;
using F6M08.Consistencia.Tests.Infra;

namespace F6M08.Consistencia.Tests;

/// <summary>
/// Passo 2 (de ponta a ponta) — fonte da verdade → fila com atraso → projeção.
/// O relógio falso torna a janela de inconsistência VISÍVEL: dá para afirmar "agora está velho" e "agora convergiu".
/// </summary>
public sealed class JanelaDeInconsistenciaTests
{
    [Fact]
    public void Escrita_SoApareceNaProjecaoDepoisDoAtraso()
    {
        var cenario = new Cenario(atrasoDaProjecao: TimeSpan.FromSeconds(3));

        var gravacao = cenario.Fonte.Criar(Eventos.Ana, 120m);

        cenario.Fonte.Obter(gravacao.PedidoId).ShouldNotBeNull("a fonte da verdade já tem o pedido");
        cenario.Projecao.ObterResumo(Eventos.Ana).Pedidos.ShouldBeEmpty("a projeção ainda não sabe");

        cenario.Avancar(TimeSpan.FromSeconds(2)).ShouldBe(0);
        cenario.Projecao.ObterResumo(Eventos.Ana).Pedidos.ShouldBeEmpty("2 s < 3 s: janela ainda aberta");

        cenario.Avancar(TimeSpan.FromSeconds(1)).ShouldBe(1);
        cenario.Projecao.ObterResumo(Eventos.Ana).ValorEmAberto.ShouldBe(120m);
    }

    [Fact]
    public void EntregaForaDeOrdemEDuplicada_ProjecaoConvergeParaAFonte()
    {
        var cenario = new Cenario(atrasoDaProjecao: TimeSpan.FromSeconds(1));
        // O PedidoCriado demora 10 s; os demais chegam em 1 s (antes dele). Todo ItemAdicionado chega duas vezes.
        cenario.Fila.AtrasoPorMensagem = e => e is PedidoCriado ? TimeSpan.FromSeconds(10) : null;
        cenario.Fila.Duplicar = e => e is ItemAdicionado;

        var pedido = cenario.Fonte.Criar(Eventos.Ana, 100m).PedidoId;
        cenario.Fonte.AdicionarItem(pedido, 30m);
        cenario.Fonte.AdicionarItem(pedido, 20m);
        cenario.Fonte.Confirmar(pedido);

        cenario.Avancar(TimeSpan.FromSeconds(1)).ShouldBe(5);
        cenario.Projecao.ObterPedido(pedido).ShouldBeNull("tudo adiado esperando a v1");
        cenario.Projecao.EventosAdiados.ShouldBe(3);

        cenario.Avancar(TimeSpan.FromSeconds(9)).ShouldBe(1);

        var verdade = cenario.Fonte.Obter(pedido)!;
        var projetado = cenario.Projecao.ObterPedido(pedido)!;
        projetado.Total.ShouldBe(150m);
        projetado.Status.ShouldBe(StatusPedido.Confirmado);
        projetado.Versao.ShouldBe(verdade.Versao);
        cenario.Projecao.EventosAdiados.ShouldBe(0);
    }
}
