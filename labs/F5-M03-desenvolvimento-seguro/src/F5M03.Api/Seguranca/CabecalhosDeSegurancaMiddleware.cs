namespace F5M03.Api.Seguranca;

/// <summary>
/// Cabeçalhos de segurança para uma API JSON (não é site: não carrega script, estilo nem frame).
/// </summary>
public sealed class CabecalhosDeSegurancaMiddleware(RequestDelegate next)
{
    /// <summary>CSP de API: nada pode ser carregado a partir de uma resposta desta API, e ela não pode ir para um iframe.</summary>
    public const string PoliticaDeConteudo = "default-src 'none'; frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        // TODO (Passo 6 do Lab): adicione em TODA resposta
        //   X-Content-Type-Options: nosniff
        //   Content-Security-Policy: PoliticaDeConteudo
        //   (opcionais) X-Frame-Options: DENY, Referrer-Policy: no-referrer; remova Server/X-Powered-By.
        // Cuidado: o tratamento de erros chama Response.Clear() (apaga headers) antes de escrever o 500.
        // Pesquise Response.OnStarting(...).
        return next(context);
    }
}
