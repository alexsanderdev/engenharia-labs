namespace F5M02.Api.Erros;

/// <summary>
/// Padroniza TODA resposta de erro da API (RFC 9457): erros de negócio, validação, 404 de rota, 405,
/// versão não suportada (Asp.Versioning) e exceções (UseExceptionHandler). Ligado no Program.cs via
/// <c>AddProblemDetails(o =&gt; o.CustomizeProblemDetails = ProblemDetailsPadrao.Customizar)</c>.
/// </summary>
public static class ProblemDetailsPadrao
{
    /// <summary>Base dos URIs de <c>type</c> próprios do OrderFlow (documentação de cada código de erro).</summary>
    public const string TipoBase = "https://docs.orderflow.dev/erros/";

    /// <summary>
    /// Regras:
    /// <list type="number">
    /// <item><c>traceId</c>: <c>Activity.Current?.Id</c> ou <c>HttpContext.TraceIdentifier</c> (sem sobrescrever se já existir).</item>
    /// <item><c>code</c>: se ninguém definiu, use <see cref="CodigoPadrao"/> com o status do problema.</item>
    /// <item><c>type</c>: se o problema tem <c>code</c> próprio do OrderFlow (contém '.'), aponte para
    /// <see cref="TipoBase"/> + code; senão mantenha o que veio (o framework preenche com o link da RFC 9110).</item>
    /// <item><c>instance</c>: o path da requisição, se vazio.</item>
    /// <item>500: NUNCA exponha detalhes da exceção; <c>title</c> "Erro interno" e <c>detail</c> genérico que cite o traceId.</item>
    /// </list>
    /// </summary>
    public static void Customizar(ProblemDetailsContext contexto) =>
        throw new NotImplementedException(
            "TODO (passo 1): acrescente traceId, code (padrão por status), type (TipoBase + code), instance e trate o 500 genérico.");

    /// <summary>
    /// Código padrão por status para erros que não passaram pelo <see cref="Problemas"/>:
    /// 400 <c>requisicao.invalida</c>, 404 <c>recurso.nao_encontrado</c>, 405 <c>metodo.nao_permitido</c>,
    /// 415 <c>requisicao.media_type_nao_suportado</c>, 500 <c>erro.interno</c>; outros: <c>http.{status}</c>.
    /// </summary>
    public static string CodigoPadrao(int status) =>
        throw new NotImplementedException("TODO (passo 1): switch por status com os códigos da documentação acima.");
}
