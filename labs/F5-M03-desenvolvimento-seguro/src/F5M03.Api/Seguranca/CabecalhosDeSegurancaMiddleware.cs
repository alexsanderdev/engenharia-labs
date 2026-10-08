namespace F5M03.Api.Seguranca;

/// <summary>
/// Cabeçalhos de segurança para uma API JSON (não é site: não carrega script, estilo nem frame).
/// Registrado via <c>Response.OnStarting</c> para valer em TODA resposta — inclusive 404 de rota
/// e 500, depois que o tratamento de erros limpou os headers com <c>Response.Clear()</c>.
/// </summary>
public sealed class CabecalhosDeSegurancaMiddleware(RequestDelegate next)
{
    /// <summary>CSP de API: nada pode ser carregado a partir de uma resposta desta API, e ela não pode ir para um iframe.</summary>
    public const string PoliticaDeConteudo = "default-src 'none'; frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static estado =>
        {
            var headers = ((HttpContext)estado).Response.Headers;
            headers.XContentTypeOptions = "nosniff";          // navegador não "adivinha" o tipo (JSON vira HTML/script)
            headers.ContentSecurityPolicy = PoliticaDeConteudo;
            headers.XFrameOptions = "DENY";                   // navegadores antigos que não entendem frame-ancestors
            headers["Referrer-Policy"] = "no-referrer";
            headers.Remove("Server");                         // não anuncia tecnologia/versão
            headers.Remove("X-Powered-By");
            return Task.CompletedTask;
        }, context);

        return next(context);
    }
}
