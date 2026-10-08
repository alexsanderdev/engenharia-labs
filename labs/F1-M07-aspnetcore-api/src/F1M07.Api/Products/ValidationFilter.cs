namespace F1M07.Api.Products;

/// <summary>
/// Endpoint filter que valida o <see cref="ProductRequest"/> ANTES do handler rodar.
/// Se houver erros, curto-circuita com 400 (ValidationProblem) e o handler nem é chamado.
/// </summary>
public sealed class ProductValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // TODO: 1) encontre o ProductRequest em context.Arguments;
        //       2) chame ProductValidator.Validate;
        //       3) se houver erros, retorne TypedResults.ValidationProblem(errors) SEM chamar next;
        //       4) senão, siga o pipeline com await next(context).
        // Enquanto isso, o filtro só repassa a chamada (e a validação não acontece).
        return await next(context);
    }
}
