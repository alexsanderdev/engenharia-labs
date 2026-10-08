using F2M07.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace F2M07.Api.Produtos;

public static class ProdutoEndpoints
{
    public static IEndpointRouteBuilder MapProdutoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/produtos").RequireAuthorization();
        group.MapPost("/", Criar);
        group.MapGet("/{id:guid}", ObterPorId);
        return app;
    }

    /// <summary>
    /// 201 com o produto criado; 400 para entrada inválida; 409 (ProblemDetails) se o SKU já existe.
    /// </summary>
    public static async Task<Results<Created<ProdutoResponse>, ValidationProblem, ProblemHttpResult>> Criar(
        ProdutoRequest request, PedidosDbContext db, CancellationToken ct)
    {
        var erros = ProdutoValidator.Validar(request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var produto = new Produto(Guid.NewGuid(), request.Sku!.Trim(), request.Nome!.Trim(), request.Preco);
        db.Produtos.Add(produto);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ErrosDeBanco.EhViolacaoDeUnicidade(ex))
        {
            // Não dá para checar "existe SKU?" antes e confiar: duas requisições simultâneas passariam.
            // Quem garante a unicidade é o índice único; a API só traduz o erro para 409.
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "SKU já cadastrado",
                detail: $"Já existe um produto com o SKU '{produto.Sku}'.");
        }

        return TypedResults.Created($"/produtos/{produto.Id}", ProdutoResponse.De(produto));
    }

    public static async Task<Results<Ok<ProdutoResponse>, NotFound>> ObterPorId(
        Guid id, PedidosDbContext db, CancellationToken ct)
    {
        var produto = await db.Produtos.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
        return produto is null ? TypedResults.NotFound() : TypedResults.Ok(ProdutoResponse.De(produto));
    }
}
