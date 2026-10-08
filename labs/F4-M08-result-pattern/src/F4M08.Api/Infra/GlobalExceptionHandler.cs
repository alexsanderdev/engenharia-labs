using Microsoft.AspNetCore.Diagnostics;

namespace F4M08.Api.Infra;

/// <summary>
/// Rede de segurança para o que NÃO é erro de negócio: bug, banco fora, timeout.
/// Registra a exceção completa no log (com stack trace) e devolve ao cliente um
/// ProblemDetails genérico, sem mensagem nem stack da exceção, com <c>code</c> e <c>traceId</c>
/// para o suporte achar o log correspondente.
/// </summary>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public const string CodigoErroInterno = "erro_interno";
    public const string CodigoRequisicaoInvalida = "requisicao_malformada";

    /// <summary>
    /// <see cref="BadHttpRequestException"/> (JSON malformado, header obrigatório ausente...) → status da
    /// própria exceção (400), log Warning, code "requisicao_malformada".
    /// Qualquer outra → 500, log Error com a exceção, code "erro_interno", title "Erro interno",
    /// detail genérico. Nunca copie <c>exception.Message</c> para a resposta.
    /// Escreve com <see cref="IProblemDetailsService.TryWriteAsync"/> e devolve true (tratada).
    /// </summary>
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _ = (problemDetails, logger);
        throw new NotImplementedException(
            "TODO: monte um ProblemDetails (Status, Title, Detail genérico, Extensions[\"code\"]), logue com LogErroInesperado/LogRequisicaoInvalida, " +
            "defina httpContext.Response.StatusCode e escreva com problemDetails.TryWriteAsync(new ProblemDetailsContext { ... }).");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro inesperado em {Metodo} {Caminho}")]
    private static partial void LogErroInesperado(ILogger logger, Exception ex, string metodo, string caminho);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Requisição malformada em {Metodo} {Caminho}")]
    private static partial void LogRequisicaoInvalida(ILogger logger, Exception ex, string metodo, string caminho);
}
