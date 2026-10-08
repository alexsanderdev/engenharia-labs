using F4M06.Catalogo.Dominio;
using F4M06.Catalogo.Infra;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace F4M06.Catalogo.Endpoints;

public sealed record CriarProdutoRequest(string? Nome, decimal Preco);

public sealed record AlterarPrecoRequest(decimal Preco);

public sealed record ProdutoResponse(Guid Id, string Nome, decimal Preco, bool Ativo)
{
    public static ProdutoResponse De(Produto p) => new(p.Id, p.Nome, p.Preco, p.Ativo);
}

/// <summary>API HTTP do Catálogo (rotas sob <c>/catalogo</c>).</summary>
public static class ProdutosEndpoints
{
    public static void MapProdutosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/catalogo/produtos");

        grupo.MapPost("/", async (CriarProdutoRequest request, CatalogoDbContext db, CancellationToken ct) =>
        {
            var erros = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.Nome)) erros["nome"] = ["Nome é obrigatório."];
            if (request.Preco <= 0) erros["preco"] = ["Preço deve ser maior que zero."];
            if (erros.Count > 0) return Results.ValidationProblem(erros);

            var produto = new Produto(Guid.NewGuid(), request.Nome!, request.Preco);
            db.Produtos.Add(produto);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/catalogo/produtos/{produto.Id}", ProdutoResponse.De(produto));
        });

        grupo.MapGet("/{id:guid}", async (Guid id, CatalogoDbContext db, CancellationToken ct) =>
            await db.Produtos.FindAsync([id], ct) is { } produto
                ? Results.Ok(ProdutoResponse.De(produto))
                : Results.NotFound());

        grupo.MapPut("/{id:guid}/preco", async (Guid id, AlterarPrecoRequest request, CatalogoDbContext db, CancellationToken ct) =>
        {
            if (request.Preco <= 0)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["preco"] = ["Preço deve ser maior que zero."] });

            if (await db.Produtos.FindAsync([id], ct) is not { } produto) return Results.NotFound();
            produto.AlterarPreco(request.Preco);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        grupo.MapPost("/{id:guid}/desativar", async (Guid id, CatalogoDbContext db, CancellationToken ct) =>
        {
            if (await db.Produtos.FindAsync([id], ct) is not { } produto) return Results.NotFound();
            produto.Desativar();
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
