using System.Globalization;
using System.Threading.RateLimiting;
using F5M05.Api.Autenticacao;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace F5M05.Api.RateLimiting;

/// <summary>
/// Políticas de rate limiting da API (Microsoft.AspNetCore.RateLimiting).
/// Cada endpoint escolhe a sua com <c>.RequireRateLimiting(PoliticasDeLimite.X)</c>.
/// </summary>
public static class PoliticasDeLimite
{
    public const string Pedidos = "pedidos";
    public const string Consultas = "consultas";
    public const string Catalogo = "catalogo";
    public const string Relatorios = "relatorios";

    // ---------- Opções de cada algoritmo (a partir da configuração) ----------
    // QueueLimit = 0 em todos os de taxa: numa API HTTP, enfileirar segura conexão e thread
    // e o cliente nem sabe; responder 429 rápido com Retry-After é mais honesto e mais barato.

    public static FixedWindowRateLimiterOptions OpcoesJanelaFixa(JanelaFixaOptions o) => new()
    {
        PermitLimit = o.Limite,
        Window = o.Janela,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    };

    public static SlidingWindowRateLimiterOptions OpcoesJanelaDeslizante(JanelaDeslizanteOptions o) => new()
    {
        PermitLimit = o.Limite,
        Window = o.Janela,
        SegmentsPerWindow = o.Segmentos,
        QueueLimit = 0,
    };

    public static TokenBucketRateLimiterOptions OpcoesBaldeDeTokens(BaldeDeTokensOptions o) => new()
    {
        TokenLimit = o.Capacidade,
        TokensPerPeriod = o.TokensPorPeriodo,
        ReplenishmentPeriod = o.Periodo,
        QueueLimit = 0,
    };

    public static ConcurrencyLimiterOptions OpcoesConcorrencia(ConcorrenciaOptions o) => new()
    {
        PermitLimit = o.Limite,
        QueueLimit = o.Fila,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    };

    // ---------- Chaves de partição ----------

    /// <summary>"cliente:{cliente_id}" quando autenticado; senão cai para a chave do IP.</summary>
    public static string ChaveDoCliente(HttpContext contexto) =>
        contexto.User.ClienteId() is { } clienteId ? $"cliente:{clienteId}" : ChaveDoIp(contexto);

    /// <summary>
    /// "ip:{endereço}". Atrás de proxy/gateway o RemoteIpAddress é o do PROXY: configure
    /// ForwardedHeaders com KnownProxies, senão todo mundo cai na mesma partição.
    /// </summary>
    public static string ChaveDoIp(HttpContext contexto) =>
        $"ip:{contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido"}";

    // ---------- Registro ----------

    public static IServiceCollection AddLimitesDeTaxa(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LimitesDeTaxaOptions>(configuration.GetSection(LimitesDeTaxaOptions.Secao));

        services.AddRateLimiter(opcoes =>
        {
            // O padrão é 503 — errado para "você excedeu o seu limite".
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opcoes.OnRejected = EscreverRejeicaoAsync;

            // A fábrica de cada partição roda UMA vez por chave; os limites são lidos das options.
            opcoes.AddPolicy(Pedidos, contexto => RateLimitPartition.GetFixedWindowLimiter(
                ChaveDoCliente(contexto), _ => OpcoesJanelaFixa(Limites(contexto).Pedidos)));

            opcoes.AddPolicy(Consultas, contexto => RateLimitPartition.GetSlidingWindowLimiter(
                ChaveDoCliente(contexto), _ => OpcoesJanelaDeslizante(Limites(contexto).Consultas)));

            opcoes.AddPolicy(Catalogo, contexto => RateLimitPartition.GetTokenBucketLimiter(
                ChaveDoIp(contexto), _ => OpcoesBaldeDeTokens(Limites(contexto).Catalogo)));

            // Uma partição só: o recurso protegido (o banco do relatório) é global.
            opcoes.AddPolicy(Relatorios, contexto => RateLimitPartition.GetConcurrencyLimiter(
                "relatorios", _ => OpcoesConcorrencia(Limites(contexto).Relatorios)));
        });

        return services;
    }

    private static LimitesDeTaxaOptions Limites(HttpContext contexto) =>
        contexto.RequestServices.GetRequiredService<IOptions<LimitesDeTaxaOptions>>().Value;

    /// <summary>429 + Retry-After (quando o algoritmo sabe calcular) + ProblemDetails.</summary>
    public static async ValueTask EscreverRejeicaoAsync(OnRejectedContext contexto, CancellationToken ct)
    {
        var http = contexto.HttpContext;
        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        int? segundos = null;
        if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            segundos = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            http.Response.Headers.RetryAfter = segundos.Value.ToString(CultureInfo.InvariantCulture);
        }

        var politica = http.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
        var problemDetails = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Type = "https://www.rfc-editor.org/rfc/rfc6585#section-4",
                Title = "Muitas requisições",
                Detail = segundos is { } s
                    ? $"Limite da política '{politica}' excedido. Tente novamente em {s} s."
                    : $"Limite da política '{politica}' excedido. Tente novamente mais tarde.",
                Extensions = { ["politica"] = politica },
            },
        });
    }
}
