namespace F1M07.Api.Products;

/// <summary>
/// Endpoint filter que valida o <see cref="ProductRequest"/> ANTES do handler rodar.
/// Se houver erros, curto-circuita com 400 (ValidationProblem) e o handler nem é chamado.
/// </summary>
public sealed class ProductValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<ProductRequest>().FirstOrDefault();
        if (request is null)
            return TypedResults.Problem("Corpo da requisição ausente.", statusCode: StatusCodes.Status400BadRequest);

        var errors = ProductValidator.Validate(request);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        return await next(context);
    }
}
