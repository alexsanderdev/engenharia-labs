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
    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Failure => StatusCodes.Status422UnprocessableEntity,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de erro sem mapeamento HTTP."),
    };

    /// <summary>Título curto e fixo por tipo (o detalhe específico vai em <c>detail</c>).</summary>
    public static string ToTitle(this ErrorType type) => type switch
    {
        ErrorType.Validation => "Requisição inválida",
        ErrorType.NotFound => "Recurso não encontrado",
        ErrorType.Conflict => "Conflito com o estado atual do recurso",
        ErrorType.Forbidden => "Acesso negado",
        ErrorType.Failure => "Regra de negócio violada",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de erro sem mapeamento HTTP."),
    };

    /// <summary>
    /// Converte o erro em um IResult de ProblemDetails:
    /// Validation → <c>TypedResults.ValidationProblem</c> (com <c>errors</c>); demais → <c>TypedResults.Problem</c>.
    /// Ambos com status de <see cref="ToStatusCode"/>, title de <see cref="ToTitle"/>, detail = Message e extensão <c>code</c>.
    /// </summary>
    public static IResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error == Error.None)
            throw new InvalidOperationException("Error.None não é um erro: não converta sucesso em ProblemDetails.");

        var extensoes = new Dictionary<string, object?> { ["code"] = error.Code };

        if (error.Type == ErrorType.Validation)
        {
            return TypedResults.ValidationProblem(
                errors: error.ValidationErrors?.ToDictionary() ?? [],
                detail: error.Message,
                title: error.Type.ToTitle(),
                extensions: extensoes);
        }

        return TypedResults.Problem(
            detail: error.Message,
            statusCode: error.Type.ToStatusCode(),
            title: error.Type.ToTitle(),
            extensions: extensoes);
    }

    /// <summary>Atalho para um <see cref="Result"/> que falhou.</summary>
    public static IResult ToProblem(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Error.ToProblem();
    }
}
