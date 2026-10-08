using F4M08.Api.Resultados;

namespace F4M08.Api.Http;

/// <summary>
/// Tradução Error → HTTP (RFC 9457 ProblemDetails). Fica na BORDA: o domínio não sabe que existe HTTP.
/// Todo problem devolvido tem, além dos campos padrão (type, title, status, detail):
/// a extensão <c>code</c> (código estável do erro) e, para validação, <c>errors</c> (por campo).
/// O <c>traceId</c> é acrescentado pelo IProblemDetailsService (ver Program.cs).
/// </summary>
public static class ErrorHttpMapping
{
    /// <summary>
    /// Validation → 400, NotFound → 404, Conflict → 409, Forbidden → 403,
    /// Failure (regra de negócio violada por requisição bem formada) → 422.
    /// </summary>
    public static int ToStatusCode(this ErrorType type) =>
        throw new NotImplementedException("TODO: switch expression ErrorType → StatusCodes.Status4xx (veja o resumo acima).");

    /// <summary>Título curto e fixo por tipo (o detalhe específico vai em <c>detail</c>).</summary>
    public static string ToTitle(this ErrorType type) =>
        throw new NotImplementedException("TODO: um título curto por tipo, ex.: \"Recurso não encontrado\".");

    /// <summary>
    /// Converte o erro em um IResult de ProblemDetails:
    /// Validation → <c>TypedResults.ValidationProblem</c> (com <c>errors</c>); demais → <c>TypedResults.Problem</c>.
    /// Ambos com status de <see cref="ToStatusCode"/>, title de <see cref="ToTitle"/>, detail = Message e extensão <c>code</c>.
    /// <see cref="Error.None"/> → <see cref="InvalidOperationException"/> (sucesso não vira problem).
    /// </summary>
    public static IResult ToProblem(this Error error) =>
        throw new NotImplementedException(
            "TODO: extensions = { [\"code\"] = error.Code }; Validation → TypedResults.ValidationProblem(errors, detail, title, extensions: ...); " +
            "demais → TypedResults.Problem(detail, statusCode, title, extensions: ...).");

    /// <summary>Atalho para um <see cref="Result"/> que falhou.</summary>
    public static IResult ToProblem(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Error.ToProblem();
    }
}
