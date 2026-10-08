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
    public static bool EhIdempotente(HttpRequestMessage requisicao) =>
        throw new NotImplementedException("TODO (Passo 1): métodos idempotentes da RFC 9110 ou header Idempotency-Key preenchido.");

    /// <summary>
    /// Repetir SOMENTE se a falha for transitória (5xx, 408, 429, <c>HttpRequestException</c>,
    /// <c>TimeoutRejectedException</c>; use <c>HttpClientResiliencePredicates.IsTransient</c>) E a
    /// requisição for idempotente. Sem requisição conhecida ou com o chamador cancelando, não repete.
    /// </summary>
    public static bool DeveRetentar(Outcome<HttpResponseMessage> resultado, HttpRequestMessage? requisicao, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO (Passo 1): transitório (HttpClientResiliencePredicates.IsTransient) E idempotente (EhIdempotente).");

    /// <summary>
    /// Retry: <c>MaxRetentativas</c>, backoff exponencial com jitter a partir de <c>AtrasoBase</c> e teto
    /// <c>AtrasoMaximo</c>, respeitando <c>Retry-After</c>, com <c>ShouldHandle</c> usando
    /// <see cref="DeveRetentar"/> (a requisição vem de <c>args.Context.GetRequestMessage()</c>).
    /// </summary>
    public static HttpRetryStrategyOptions CriarRetry(GatewayPagamentoOptions o) =>
        throw new NotImplementedException("TODO (Passo 1): new HttpRetryStrategyOptions { MaxRetryAttempts, BackoffType, UseJitter, Delay, MaxDelay, ShouldRetryAfterHeader, ShouldHandle }.");

    /// <summary>Circuit breaker com taxa, vazão mínima, janela e duração aberto vindos das options.</summary>
    public static HttpCircuitBreakerStrategyOptions CriarCircuitBreaker(GatewayPagamentoOptions o) =>
        throw new NotImplementedException("TODO (Passo 1): new HttpCircuitBreakerStrategyOptions { FailureRatio, MinimumThroughput, SamplingDuration, BreakDuration }.");

    /// <summary>Timeout do orçamento total (estratégia mais externa).</summary>
    public static HttpTimeoutStrategyOptions CriarTimeoutTotal(GatewayPagamentoOptions o) =>
        throw new NotImplementedException("TODO (Passo 1): new HttpTimeoutStrategyOptions { Timeout = o.TimeoutTotal }.");

    /// <summary>Timeout de cada tentativa (estratégia mais interna).</summary>
    public static HttpTimeoutStrategyOptions CriarTimeoutPorTentativa(GatewayPagamentoOptions o) =>
        throw new NotImplementedException("TODO (Passo 1): new HttpTimeoutStrategyOptions { Timeout = o.TimeoutPorTentativa }.");

    /// <summary>Monta o pipeline na ordem: timeout total → retry → circuit breaker → timeout por tentativa.</summary>
    public static void Configurar(ResiliencePipelineBuilder<HttpResponseMessage> builder, GatewayPagamentoOptions o) =>
        throw new NotImplementedException("TODO (Passo 5): builder.AddTimeout(total).AddRetry(...).AddCircuitBreaker(...).AddTimeout(porTentativa).");
}
