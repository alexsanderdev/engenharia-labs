using F6M08.Consistencia.Dominio;
using F6M08.Consistencia.Reconciliacao;
using F6M08.Consistencia.Tests.Infra;
using static F6M08.Consistencia.Tests.Infra.Eventos;

namespace F6M08.Consistencia.Tests;

/// <summary>
/// Passo 4 — reconciliação: a fonte da verdade vence, a projeção nunca anda para trás e
/// divergência recente (provavelmente em trânsito) não é "corrigida".
/// </summary>
public sealed class ReconciliacaoTests
{
    private static readonly TimeSpan Tolerancia = TimeSpan.FromSeconds(30);

    private static Cenario NovoCenario() =>
        new(atrasoDaProjecao: TimeSpan.FromSeconds(1), configurar: o => o.ToleranciaDaReconciliacao = Tolerancia);

    [Fact]
    public void ProjecaoEmDia_NaoCorrigeNada()
    {
        var cenario = NovoCenario();
        cenario.Fonte.Criar(Ana, 10m);
        cenario.Fonte.Criar(Bruno, 20m);
        cenario.Avancar(Tolerancia);

        var relatorio = cenario.Reconciliador().Reconciliar();

        relatorio.Verificados.ShouldBe(2);
        relatorio.Corrigidas.ShouldBeEmpty();
        relatorio.AdiadasPorTolerancia.ShouldBe(0);
    }

    [Fact]
    public void EventoPerdido_DepoisDaTolerancia_CorrigeAPartirDaFonte()
    {
        var cenario = NovoCenario();
        cenario.Fila.Perder = e => e is PedidoCriado;
        var gravacao = cenario.Fonte.Criar(Ana, 99m);
        cenario.Avancar(Tolerancia);
        cenario.Projecao.ObterPedido(gravacao.PedidoId).ShouldBeNull("o evento se perdeu: nenhum retry vai trazê-lo");

        var relatorio = cenario.Reconciliador().Reconciliar();

        relatorio.Corrigidas.ShouldBe([new Divergencia(gravacao.PedidoId, TipoDeDivergencia.Ausente, 0, 1)]);
        cenario.Projecao.ObterResumo(Ana).ValorEmAberto.ShouldBe(99m);
    }

    [Fact]
    public void DivergenciaRecente_DentroDaTolerancia_NaoMexe()
    {
        var cenario = NovoCenario();
        var pedido = cenario.Fonte.Criar(Ana, 10m).PedidoId;
        cenario.Avancar(Tolerancia);
        cenario.Fonte.Confirmar(pedido); // v2 em trânsito (chega em 1 s)

        var relatorio = cenario.Reconciliador().Reconciliar();

        relatorio.Corrigidas.ShouldBeEmpty();
        relatorio.AdiadasPorTolerancia.ShouldBe(1);
        cenario.Projecao.VersaoDoPedido(pedido).ShouldBe(1, "quem entrega a v2 é o fluxo normal, não o job");

        cenario.Avancar(TimeSpan.FromSeconds(1));
        cenario.Projecao.ObterPedido(pedido)!.Status.ShouldBe(StatusPedido.Confirmado);
    }

    [Fact]
    public void LacunaPresa_ReconciliadorDestravaEOFluxoNormalContinua()
    {
        var cenario = NovoCenario();
        var pedido = cenario.Fonte.Criar(Ana, 100m).PedidoId;
        cenario.Fila.Perder = e => e is ItemAdicionado { Versao: 2 };
        cenario.Fonte.AdicionarItem(pedido, 10m); // v2: perdido
        cenario.Fonte.AdicionarItem(pedido, 5m);  // v3: fica adiado para sempre
        cenario.Avancar(Tolerancia);
        cenario.Projecao.EventosAdiados.ShouldBe(1);
        cenario.Projecao.ObterPedido(pedido)!.Total.ShouldBe(100m);

        var relatorio = cenario.Reconciliador().Reconciliar();

        relatorio.Corrigidas.ShouldBe([new Divergencia(pedido, TipoDeDivergencia.Atrasado, 1, 3)]);
        cenario.Projecao.ObterPedido(pedido).ShouldBe(new Projecao.PedidoResumido(pedido, Ana, StatusPedido.Criado, 115m, 3));
        cenario.Projecao.EventosAdiados.ShouldBe(0, "a v3 adiada já está coberta pelo estado da fonte");

        cenario.Fila.Perder = null;
        cenario.Fonte.Confirmar(pedido); // v4 segue pelo fluxo normal
        cenario.Avancar(TimeSpan.FromSeconds(1));
        cenario.Projecao.ObterPedido(pedido)!.Status.ShouldBe(StatusPedido.Confirmado);
    }

    [Fact]
    public void PedidoFantasma_SoNaProjecao_ERemovido()
    {
        var cenario = NovoCenario();
        var fantasma = Guid.NewGuid();
        cenario.Projecao.Aplicar(Criado(fantasma, 500m)); // publicado por uma escrita que sofreu rollback (sem Outbox)
        cenario.Avancar(Tolerancia);

        var relatorio = cenario.Reconciliador().Reconciliar();

        relatorio.Corrigidas.ShouldBe([new Divergencia(fantasma, TipoDeDivergencia.Fantasma, 1, 0)]);
        cenario.Projecao.ObterResumo(Ana).Pedidos.ShouldBeEmpty();
    }
}
