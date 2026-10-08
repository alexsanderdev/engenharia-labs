using System.Threading.RateLimiting;
using F5M05.Api.RateLimiting;

namespace F5M05.Api.Tests;

/// <summary>
/// Parte (a), Passo 1 — as opções de cada algoritmo e a diferença de comportamento entre eles.
/// Como os limitadores não aceitam TimeProvider, o "tempo" aqui é dado à mão: os testes criam o
/// limitador com <c>AutoReplenishment = false</c> e uma janela de poucos ticks, e cada
/// <c>TryReplenish()</c> equivale a "passou um segmento/uma janela". Nada de Sleep.
/// </summary>
public sealed class AlgoritmosTests
{
    [Fact]
    public void Opcoes_TodosOsAlgoritmosDeTaxa_RejeitamNaHoraSemFila()
    {
        var fixa = PoliticasDeLimite.OpcoesJanelaFixa(new() { Limite = 7, Janela = TimeSpan.FromSeconds(30) });
        var deslizante = PoliticasDeLimite.OpcoesJanelaDeslizante(new() { Limite = 8, Janela = TimeSpan.FromMinutes(1), Segmentos = 6 });
        var balde = PoliticasDeLimite.OpcoesBaldeDeTokens(new() { Capacidade = 9, TokensPorPeriodo = 3, Periodo = TimeSpan.FromSeconds(10) });
        var concorrencia = PoliticasDeLimite.OpcoesConcorrencia(new() { Limite = 2, Fila = 0 });

        (fixa.PermitLimit, fixa.Window, fixa.QueueLimit).ShouldBe((7, TimeSpan.FromSeconds(30), 0));
        (deslizante.PermitLimit, deslizante.Window, deslizante.SegmentsPerWindow, deslizante.QueueLimit)
            .ShouldBe((8, TimeSpan.FromMinutes(1), 6, 0));
        (balde.TokenLimit, balde.TokensPerPeriod, balde.ReplenishmentPeriod, balde.QueueLimit)
            .ShouldBe((9, 3, TimeSpan.FromSeconds(10), 0));
        (concorrencia.PermitLimit, concorrencia.QueueLimit).ShouldBe((2, 0));
    }

    [Fact]
    public void JanelaFixa_NaViradaDaJanela_PermiteORajadaDobrada()
    {
        var opcoes = PoliticasDeLimite.OpcoesJanelaFixa(new() { Limite = 4, Janela = TimeSpan.FromTicks(1) });
        opcoes.AutoReplenishment = false;
        using var limitador = new FixedWindowRateLimiter(opcoes);

        var fimDaJanela1 = Enumerable.Range(0, 4).Count(_ => limitador.AttemptAcquire().IsAcquired);
        limitador.AttemptAcquire().IsAcquired.ShouldBeFalse("o 5º da janela é rejeitado");
        limitador.TryReplenish(); // virou a janela
        var inicioDaJanela2 = Enumerable.Range(0, 4).Count(_ => limitador.AttemptAcquire().IsAcquired);

        (fimDaJanela1 + inicioDaJanela2).ShouldBe(8, "fim de uma janela + início da outra: o dobro do limite em instantes");
    }

    [Fact]
    public void JanelaDeslizante_SoDevolvePermissoesQuandoOSegmentoAntigoSaiDaJanela()
    {
        // Janela de 2 segmentos: cada TryReplenish avança um segmento.
        var opcoes = PoliticasDeLimite.OpcoesJanelaDeslizante(new() { Limite = 4, Janela = TimeSpan.FromTicks(2), Segmentos = 2 });
        opcoes.AutoReplenishment = false;
        using var limitador = new SlidingWindowRateLimiter(opcoes);

        Enumerable.Range(0, 4).Count(_ => limitador.AttemptAcquire().IsAcquired).ShouldBe(4); // rajada no segmento 0
        limitador.TryReplenish();                                                              // segmento 1
        limitador.AttemptAcquire().IsAcquired.ShouldBeFalse("o segmento 0 ainda está dentro da janela");
        limitador.TryReplenish();                                                              // segmento 2: o 0 saiu
        Enumerable.Range(0, 4).Count(_ => limitador.AttemptAcquire().IsAcquired).ShouldBe(4);
    }

    [Fact]
    public void BaldeDeTokens_PermiteRajadaAteACapacidadeEDepoisRejeita()
    {
        var opcoes = PoliticasDeLimite.OpcoesBaldeDeTokens(new() { Capacidade = 3, TokensPorPeriodo = 1, Periodo = TimeSpan.FromHours(1) });
        opcoes.AutoReplenishment = false;
        using var limitador = new TokenBucketRateLimiter(opcoes);

        limitador.AttemptAcquire(3).IsAcquired.ShouldBeTrue("o balde cheio absorve a rajada");
        var rejeitada = limitador.AttemptAcquire();

        rejeitada.IsAcquired.ShouldBeFalse();
        rejeitada.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter).ShouldBeTrue();
        retryAfter.ShouldBe(TimeSpan.FromHours(1), "falta 1 período para o próximo token");
    }

    [Fact]
    public void Concorrencia_LiberaAPermissaoQuandoALeaseEDescartada()
    {
        using var limitador = new ConcurrencyLimiter(PoliticasDeLimite.OpcoesConcorrencia(new() { Limite = 1, Fila = 0 }));

        var primeira = limitador.AttemptAcquire();
        primeira.IsAcquired.ShouldBeTrue();
        limitador.AttemptAcquire().IsAcquired.ShouldBeFalse("uma por vez");

        primeira.Dispose(); // requisição terminou
        limitador.AttemptAcquire().IsAcquired.ShouldBeTrue();
    }
}
