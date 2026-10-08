namespace F5M03.Api.Infra;

/// <summary>
/// Log de acesso.
/// VULNERÁVEL — corrija (Passo 5 do Lab).
/// </summary>
public sealed class LogDeRequisicaoMiddleware(RequestDelegate next, ILogger<LogDeRequisicaoMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // VULNERÁVEL: loga TODOS os headers (Authorization, Cookie, X-Api-Key...) e a query string.
        // TODO: logue só uma ALLOWLIST de headers inofensivos (User-Agent, Content-Type, Content-Length,
        //       Accept, X-Correlation-Id) e não logue a query string.
        var cabecalhos = string.Join("; ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"));

        logger.LogInformation("HTTP {Metodo} {Caminho}{Query} Headers: {Headers}",
            context.Request.Method, context.Request.Path, context.Request.QueryString, cabecalhos);

        await next(context);
    }
}
