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
        group.MapGet("/{id:guid}", GetById).WithName("GetProductById");

        // Os filtros abaixo valem só para estes dois endpoints.
        group.MapPost("/", Create).AddEndpointFilter<ProductValidationFilter>();
        group.MapPut("/{id:guid}", Update).AddEndpointFilter<ProductValidationFilter>();

        group.MapPost("/{id:guid}/activate", Activate);
        group.MapPost("/{id:guid}/deactivate", Deactivate);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    /// <summary>200 com todos os produtos (ordenados por nome).</summary>
    public static async Task<Ok<ProductResponse[]>> GetAll(IProductRepository repo, CancellationToken ct)
    {
        var products = await repo.GetAllAsync(ct);
        return TypedResults.Ok(products.Select(ProductResponse.From).ToArray());
    }

    /// <summary>200 com o produto ou 404.</summary>
    public static async Task<Results<Ok<ProductResponse>, NotFound>> GetById(Guid id, IProductRepository repo, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(id, ct);
        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ProductResponse.From(product));
    }

    /// <summary>
    /// Cria o produto (ativo, nome com Trim) e devolve 201 com header Location "/products/{id}".
    /// A validação já foi feita pelo filtro; o ValidationProblem no tipo de retorno documenta o 400 no OpenAPI.
    /// </summary>
    public static async Task<Results<Created<ProductResponse>, ValidationProblem>> Create(
        ProductRequest request, IProductRepository repo, CancellationToken ct)
    {
        var product = new Product(Guid.NewGuid(), request.Name!.Trim(), request.Price, IsActive: true);
        await repo.AddAsync(product, ct);
        return TypedResults.Created($"/products/{product.Id}", ProductResponse.From(product));
    }

    /// <summary>Altera nome e preço (mantém IsActive). 204 se alterou, 404 se não existe.</summary>
    public static async Task<Results<NoContent, NotFound, ValidationProblem>> Update(
        Guid id, ProductRequest request, IProductRepository repo, CancellationToken ct)
    {
        var current = await repo.GetByIdAsync(id, ct);
        if (current is null)
            return TypedResults.NotFound();

        var updated = current with { Name = request.Name!.Trim(), Price = request.Price };
        return await repo.UpdateAsync(updated, ct) ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    /// <summary>Marca o produto como ativo. 204 ou 404.</summary>
    public static Task<Results<NoContent, NotFound>> Activate(Guid id, IProductRepository repo, CancellationToken ct) =>
        SetActive(id, true, repo, ct);

    /// <summary>Marca o produto como inativo. 204 ou 404.</summary>
    public static Task<Results<NoContent, NotFound>> Deactivate(Guid id, IProductRepository repo, CancellationToken ct) =>
        SetActive(id, false, repo, ct);

    /// <summary>Remove o produto. 204 ou 404.</summary>
    public static async Task<Results<NoContent, NotFound>> Delete(Guid id, IProductRepository repo, CancellationToken ct) =>
        await repo.RemoveAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> SetActive(Guid id, bool active, IProductRepository repo, CancellationToken ct)
    {
        var current = await repo.GetByIdAsync(id, ct);
        if (current is null)
            return TypedResults.NotFound();

        return await repo.UpdateAsync(current with { IsActive = active }, ct)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
