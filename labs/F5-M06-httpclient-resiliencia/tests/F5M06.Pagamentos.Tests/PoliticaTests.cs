using System.Net;
using F5M06.Pagamentos.Gateway;
using Polly;

namespace F5M06.Pagamentos.Tests;

/// <summary>Passo 1 — as regras do pipeline como funções puras (sem HTTP).</summary>
public sealed class PoliticaTests
{
    [Theory]
    [InlineData("GET", 503, false, true)]
    [InlineData("GET", 500, false, true)]
    [InlineData("GET", 429, false, true)]
    [InlineData("GET", 408, false, true)]
    [InlineData("GET", 404, false, false)]
    [InlineData("GET", 200, false, false)]
    [InlineData("PUT", 502, false, true)]
    [InlineData("POST", 503, false, false)]
    [InlineData("POST", 503, true, true)]
    [InlineData("POST", 400, true, false)]
    [InlineData("POST", 0, false, false)]
    [InlineData("POST", 0, true, true)]
    public void DeveRetentar_SoFalhaTransitoriaEmOperacaoIdempotente(string metodo, int status, bool comChave, bool esperado)
    {
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), "v1/cobrancas");
        if (comChave)
        {
            requisicao.Headers.Add(ResilienciaGateway.CabecalhoIdempotencia, "pedido-1-pagamento-1");
        }

        using var resposta = status == 0 ? null : new HttpResponseMessage((HttpStatusCode)status);
        var resultado = resposta is null
            ? Outcome.FromException<HttpResponseMessage>(new HttpRequestException("conexão recusada")) // 0 = falha de rede
            : Outcome.FromResult(resposta);

        ResilienciaGateway.DeveRetentar(resultado, requisicao).ShouldBe(esperado);
        ResilienciaGateway.DeveRetentar(resultado, requisicao: null).ShouldBeFalse("sem saber a requisição, não arrisque repetir");
    }

    [Fact]
    public void Fabricas_TraduzemAsOptionsParaAsEstrategiasDoPolly()
    {
        var o = new GatewayPagamentoOptions
        {
            MaxRetentativas = 4,
            AtrasoBase = TimeSpan.FromMilliseconds(250),
            AtrasoMaximo = TimeSpan.FromSeconds(3),
            TimeoutPorTentativa = TimeSpan.FromSeconds(2),
            TimeoutTotal = TimeSpan.FromSeconds(12),
            CircuitoTaxaDeFalhas = 0.4,
            CircuitoVazaoMinima = 8,
            CircuitoJanela = TimeSpan.FromSeconds(20),
            CircuitoDuracaoAberto = TimeSpan.FromSeconds(7),
        };

        var retry = ResilienciaGateway.CriarRetry(o);
        retry.MaxRetryAttempts.ShouldBe(4);
        retry.BackoffType.ShouldBe(DelayBackoffType.Exponential);
        retry.UseJitter.ShouldBeTrue("sem jitter, clientes que falharam juntos voltam juntos (retry storm)");
        retry.Delay.ShouldBe(TimeSpan.FromMilliseconds(250));
        retry.MaxDelay.ShouldBe(TimeSpan.FromSeconds(3));
        retry.ShouldRetryAfterHeader.ShouldBeTrue();

        var circuito = ResilienciaGateway.CriarCircuitBreaker(o);
        circuito.FailureRatio.ShouldBe(0.4);
        circuito.MinimumThroughput.ShouldBe(8);
        circuito.SamplingDuration.ShouldBe(TimeSpan.FromSeconds(20));
        circuito.BreakDuration.ShouldBe(TimeSpan.FromSeconds(7));

        ResilienciaGateway.CriarTimeoutPorTentativa(o).Timeout.ShouldBe(TimeSpan.FromSeconds(2));
        ResilienciaGateway.CriarTimeoutTotal(o).Timeout.ShouldBe(TimeSpan.FromSeconds(12));
    }
}
