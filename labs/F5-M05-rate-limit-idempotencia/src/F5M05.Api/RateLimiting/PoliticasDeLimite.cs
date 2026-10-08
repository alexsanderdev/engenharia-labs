using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

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

    // ---------- Opções de cada algoritmo (Passo 1) ----------

    /// <summary>Janela fixa: PermitLimit = Limite, Window = Janela, sem fila (QueueLimit = 0).</summary>
    public static FixedWindowRateLimiterOptions OpcoesJanelaFixa(JanelaFixaOptions o) =>
        throw new NotImplementedException("TODO: new FixedWindowRateLimiterOptions { PermitLimit, Window, QueueLimit = 0 }.");

    /// <summary>Janela deslizante: PermitLimit, Window, SegmentsPerWindow = Segmentos, sem fila.</summary>
    public static SlidingWindowRateLimiterOptions OpcoesJanelaDeslizante(JanelaDeslizanteOptions o) =>
        throw new NotImplementedException("TODO: new SlidingWindowRateLimiterOptions { PermitLimit, Window, SegmentsPerWindow, QueueLimit = 0 }.");

    /// <summary>Balde de tokens: TokenLimit = Capacidade, TokensPerPeriod, ReplenishmentPeriod = Periodo, sem fila.</summary>
    public static TokenBucketRateLimiterOptions OpcoesBaldeDeTokens(BaldeDeTokensOptions o) =>
        throw new NotImplementedException("TODO: new TokenBucketRateLimiterOptions { TokenLimit, TokensPerPeriod, ReplenishmentPeriod, QueueLimit = 0 }.");

    /// <summary>Concorrência: PermitLimit = Limite, QueueLimit = Fila.</summary>
    public static ConcurrencyLimiterOptions OpcoesConcorrencia(ConcorrenciaOptions o) =>
        throw new NotImplementedException("TODO: new ConcurrencyLimiterOptions { PermitLimit, QueueLimit }.");

    // ---------- Chaves de partição (Passo 2) ----------

    /// <summary>"cliente:{cliente_id}" quando autenticado; senão cai para a chave do IP.</summary>
    public static string ChaveDoCliente(HttpContext contexto) =>
        throw new NotImplementedException("TODO: use contexto.User.ClienteId() (pasta Autenticacao) ou ChaveDoIp.");

    /// <summary>"ip:{endereço}" a partir de Connection.RemoteIpAddress ("ip:desconhecido" se nulo).</summary>
    public static string ChaveDoIp(HttpContext contexto) =>
        throw new NotImplementedException("TODO: \"ip:\" + contexto.Connection.RemoteIpAddress.");

    // ---------- Registro (Passos 2 e 3) ----------

    public static IServiceCollection AddLimitesDeTaxa(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LimitesDeTaxaOptions>(configuration.GetSection(LimitesDeTaxaOptions.Secao));

        services.AddRateLimiter(opcoes =>
        {
            // TODO (Passo 3): RejectionStatusCode (o padrão é 503!) e OnRejected = EscreverRejeicaoAsync.
            // TODO (Passo 2): uma política por nome, lendo os limites de IOptions<LimitesDeTaxaOptions>
            //   (contexto.RequestServices) dentro do particionador:
            //   - Pedidos    → RateLimitPartition.GetFixedWindowLimiter(ChaveDoCliente, ... OpcoesJanelaFixa)
            //   - Consultas  → GetSlidingWindowLimiter(ChaveDoCliente, ... OpcoesJanelaDeslizante)
            //   - Catalogo   → GetTokenBucketLimiter(ChaveDoIp, ... OpcoesBaldeDeTokens)
            //   - Relatorios → GetConcurrencyLimiter(chave única, ... OpcoesConcorrencia)
            _ = opcoes;
        });

        return services;
    }

    /// <summary>
    /// 429 + header Retry-After em segundos inteiros, arredondado para cima (quando a lease tem
    /// MetadataName.RetryAfter) + ProblemDetails (status 429) via IProblemDetailsService.
    /// </summary>
    public static ValueTask EscreverRejeicaoAsync(OnRejectedContext contexto, CancellationToken ct) =>
        throw new NotImplementedException("TODO: status 429, Retry-After e ProblemDetails.");
}
