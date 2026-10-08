namespace F5M03.Api.Infra;

/// <summary>
/// Log de acesso. Allowlist de cabeçalhos: só entra no log o que sabidamente não é segredo.
/// (Denylist — "tirar Authorization e Cookie" — falha no dia em que alguém inventa X-Api-Key.)
/// A query string também fica de fora: tokens e dados pessoais adoram aparecer nela.
/// </summary>
public sealed class LogDeRequisicaoMiddleware(RequestDelegate next, ILogger<LogDeRequisicaoMiddleware> logger)
{
    private static readonly string[] CabecalhosSeguros = ["User-Agent", "Content-Type", "Content-Length", "Accept", "X-Correlation-Id"];

    public async Task InvokeAsync(HttpContext context)
    {
        var cabecalhos = string.Join("; ", CabecalhosSeguros
            .Where(nome => context.Request.Headers.ContainsKey(nome))
            .Select(nome => $"{nome}={context.Request.Headers[nome]}"));

        logger.LogInformation("HTTP {Metodo} {Caminho} Headers: {Headers}",
            context.Request.Method, context.Request.Path, cabecalhos);

        await next(context);
    }
}
