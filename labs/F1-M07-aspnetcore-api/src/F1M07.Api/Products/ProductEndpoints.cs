using Microsoft.AspNetCore.Http.HttpResults;

namespace F1M07.Api.Products;

/// <summary>Endpoints de produtos organizados em um route group.</summary>
public static class ProductEndpoints
{
    /// <summary>
    /// Mapeia o grupo "/products":
    /// GET "/" (lista), GET "/{id:guid}" (detalhe), POST "/" (cria), PUT "/{id:guid}" (altera),
    /// POST "/{id:guid}/activate", POST "/{id:guid}/deactivate", DELETE "/{id:guid}".
    /// POST e PUT passam pelo <see cref="ProductValidationFilter"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products").WithTags("Products");

        group.MapGet("/", GetAll).WithName("GetProducts");

        // TODO: mapeie os demais endpoints no grupo:
        //   GET    "/{id:guid}"            -> GetById
        //   POST   "/"                     -> Create  (+ AddEndpointFilter<ProductValidationFilter>())
        //   PUT    "/{id:guid}"            -> Update  (+ AddEndpointFilter<ProductValidationFilter>())
        //   POST   "/{id:guid}/activate"   -> Activate
        //   POST   "/{id:guid}/deactivate" -> Deactivate
        //   DELETE "/{id:guid}"            -> Delete

        return app;
    }

    /// <summary>200 com todos os produtos (ordenados por nome).</summary>
    public static async Task<Ok<ProductResponse[]>> GetAll(IProductRepository repo, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: busque no repositório e devolva TypedResults.Ok com ProductResponse[].");
    }

    /// <summary>200 com o produto ou 404.</summary>
    public static async Task<Results<Ok<ProductResponse>, NotFound>> GetById(Guid id, IProductRepository repo, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: devolva TypedResults.Ok(...) ou TypedResults.NotFound().");
    }

    /// <summary>
    /// Cria o produto (ativo, nome com Trim) e devolve 201 com header Location "/products/{id}".
    /// A validação já foi feita pelo filtro; o ValidationProblem no tipo de retorno documenta o 400 no OpenAPI.
    /// </summary>
    public static async Task<Results<Created<ProductResponse>, ValidationProblem>> Create(
        ProductRequest request, IProductRepository repo, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: crie o Product, salve e devolva TypedResults.Created($\"/products/{id}\", dto).");
    }

    /// <summary>Altera nome e preço (mantém IsActive). 204 se alterou, 404 se não existe.</summary>
    public static async Task<Results<NoContent, NotFound, ValidationProblem>> Update(
        Guid id, ProductRequest request, IProductRepository repo, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: busque, aplique 'with { Name, Price }', salve e devolva NoContent ou NotFound.");
    }

    /// <summary>Marca o produto como ativo. 204 ou 404.</summary>
    public static Task<Results<NoContent, NotFound>> Activate(Guid id, IProductRepository repo, CancellationToken ct) =>
        throw new NotImplementedException("TODO: marque IsActive = true (204) ou devolva 404.");

    /// <summary>Marca o produto como inativo. 204 ou 404.</summary>
    public static Task<Results<NoContent, NotFound>> Deactivate(Guid id, IProductRepository repo, CancellationToken ct) =>
        throw new NotImplementedException("TODO: marque IsActive = false (204) ou devolva 404.");

    /// <summary>Remove o produto. 204 ou 404.</summary>
    public static async Task<Results<NoContent, NotFound>> Delete(Guid id, IProductRepository repo, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: remova pelo repositório e devolva NoContent ou NotFound.");
    }
}
