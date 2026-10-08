using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace F4M05.Api.Comum;

/// <summary>Recurso inexistente → 404.</summary>
public sealed class NaoEncontradoException(string recurso, Guid id) : Exception($"{recurso} não encontrado: {id}");

/// <summary>Regra de negócio violada (ex.: produto inativo) → 422.</summary>
public class RegraDeNegocioException(string message) : Exception(message);

/// <summary>Transição de status inválida (ex.: confirmar pedido cancelado) → 409.</summary>
public sealed class TransicaoInvalidaException(string message) : RegraDeNegocioException(message);

/// <summary>
/// Tradução ÚNICA de exceções para ProblemDetails. Código transversal de verdade:
/// é o tipo de coisa que DEVE ser compartilhada entre fatias.
/// </summary>
internal sealed class TratamentoDeErros(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problema = exception switch
        {
            ValidationException v => new HttpValidationProblemDetails(
                v.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Requisição inválida",
            },
            NaoEncontradoException => Problema(StatusCodes.Status404NotFound, "Não encontrado", exception),
            TransicaoInvalidaException => Problema(StatusCodes.Status409Conflict, "Transição de status inválida", exception),
            RegraDeNegocioException => Problema(StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", exception),
            _ => null,
        };
        if (problema is null) return false;

        httpContext.Response.StatusCode = problema.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problema,
        });
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails Problema(int status, string titulo, Exception ex) =>
        new() { Status = status, Title = titulo, Detail = ex.Message };
}
