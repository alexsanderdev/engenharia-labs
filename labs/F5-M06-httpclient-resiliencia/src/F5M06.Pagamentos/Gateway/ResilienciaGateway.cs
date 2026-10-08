using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace F5M06.Pagamentos.Gateway;

/// <summary>
/// Pipeline de resiliência do gateway (Polly v8 via Microsoft.Extensions.Http.Resilience).
/// Ordem, de fora para dentro: timeout total → retry → circuit breaker → timeout por tentativa.
/// </summary>
public static class ResilienciaGateway
{
    /// <summary>Nome do pipeline (aparece na telemetria do Polly).</summary>
    public const string NomePipeline = "gateway-pagamento";

    /// <summary>Header que torna um POST seguro para repetir.</summary>
    public const string CabecalhoIdempotencia = "Idempotency-Key";

    /// <summary>
    /// Idempotente = GET, HEAD, OPTIONS, PUT, DELETE, TRACE (RFC 9110) ou qualquer método com
    /// <see cref="CabecalhoIdempotencia"/> preenchido.
    /// </summary>
    public static bool EhIdempotente(HttpRequestMessage requisicao)
    {
        ArgumentNullException.ThrowIfNull(requisicao);

        var metodo = requisicao.Method;
        if (metodo == HttpMethod.Get || metodo == HttpMethod.Head || metodo == HttpMethod.Options
            || metodo == HttpMethod.Put || metodo == HttpMethod.Delete || metodo == HttpMethod.Trace)
        {
            return true;
        }

        return requisicao.Headers.TryGetValues(CabecalhoIdempotencia, out var valores)
            && valores.Any(v => !string.IsNullOrWhiteSpace(v));
    }

    /// <summary>
    /// Repetir SOMENTE se a falha for transitória (5xx, 408, 429, <c>HttpRequestException</c>,
    /// <c>TimeoutRejectedException</c>; use <c>HttpClientResiliencePredicates.IsTransient</c>) E a
    /// requisição for idempotente. Sem requisição conhecida ou com o chamador cancelando, não repete.
    /// </summary>
    public static bool DeveRetentar(Outcome<HttpResponseMessage> resultado, HttpRequestMessage? requisicao, CancellationToken ct = default)
    {
        // A sobrecarga IsTransient(outcome, ct) ainda é experimental (EXTEXP0001); checamos o ct à mão.
#pragma warning disable CA2016
        if (requisicao is null || ct.IsCancellationRequested || !HttpClientResiliencePredicates.IsTransient(resultado))
#pragma warning restore CA2016
        {
            return false;
        }

        return EhIdempotente(requisicao);
    }

    /// <summary>
    /// Retry: <c>MaxRetentativas</c>, backoff exponencial com jitter a partir de <c>AtrasoBase</c> e teto
    /// <c>AtrasoMaximo</c>, respeitando <c>Retry-After</c>, com <c>ShouldHandle</c> usando
    /// <see cref="DeveRetentar"/> (a requisição vem de <c>args.Context.GetRequestMessage()</c>).
    /// </summary>
    public static HttpRetryStrategyOptions CriarRetry(GatewayPagamentoOptions o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return new HttpRetryStrategyOptions
        {
            Name = "retry",
            MaxRetryAttempts = o.MaxRetentativas,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = o.AtrasoBase,
            MaxDelay = o.AtrasoMaximo,
            ShouldRetryAfterHeader = true,
            ShouldHandle = args => ValueTask.FromResult(
                DeveRetentar(args.Outcome, args.Context.GetRequestMessage(), args.Context.CancellationToken)),
        };
    }

    /// <summary>Circuit breaker com taxa, vazão mínima, janela e duração aberto vindos das options.</summary>
    public static HttpCircuitBreakerStrategyOptions CriarCircuitBreaker(GatewayPagamentoOptions o)
    {
        ArgumentNullException.ThrowIfNull(o);

        return new HttpCircuitBreakerStrategyOptions
        {
            Name = "circuit-breaker",
            FailureRatio = o.CircuitoTaxaDeFalhas,
            MinimumThroughput = o.CircuitoVazaoMinima,
            SamplingDuration = o.CircuitoJanela,
            BreakDuration = o.CircuitoDuracaoAberto,
        };
    }

    /// <summary>Timeout do orçamento total (estratégia mais externa).</summary>
    public static HttpTimeoutStrategyOptions CriarTimeoutTotal(GatewayPagamentoOptions o)
    {
        ArgumentNullException.ThrowIfNull(o);
        return new HttpTimeoutStrategyOptions { Name = "timeout-total", Timeout = o.TimeoutTotal };
    }

    /// <summary>Timeout de cada tentativa (estratégia mais interna).</summary>
    public static HttpTimeoutStrategyOptions CriarTimeoutPorTentativa(GatewayPagamentoOptions o)
    {
        ArgumentNullException.ThrowIfNull(o);
        return new HttpTimeoutStrategyOptions { Name = "timeout-tentativa", Timeout = o.TimeoutPorTentativa };
    }

    /// <summary>Monta o pipeline na ordem: timeout total → retry → circuit breaker → timeout por tentativa.</summary>
    public static void Configurar(ResiliencePipelineBuilder<HttpResponseMessage> builder, GatewayPagamentoOptions o)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(o);

        builder
            .AddTimeout(CriarTimeoutTotal(o))
            .AddRetry(CriarRetry(o))
            .AddCircuitBreaker(CriarCircuitBreaker(o))
            .AddTimeout(CriarTimeoutPorTentativa(o));
    }
}
