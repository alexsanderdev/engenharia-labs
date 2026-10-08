using F4M01.Application.Pedidos;
using F4M01.Domain.Comum;
using Microsoft.AspNetCore.Diagnostics;

namespace F4M01.Api.Erros;

/// <summary>
/// Traduz exceções de negócio para HTTP (ProblemDetails). O domínio lança <see cref="DomainException"/>
/// sem saber o que é status code; a borda decide que isso vira 422.
/// </summary>
internal sealed class ErrosDeAplicacaoHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            DomainException => StatusCodes.Status422UnprocessableEntity,
            ProdutoNaoEncontradoException => StatusCodes.Status404NotFound,
            _ => 0,
        };
        if (status == 0) return false;

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = "Pedido inválido", Detail = exception.Message },
        });
    }
}
