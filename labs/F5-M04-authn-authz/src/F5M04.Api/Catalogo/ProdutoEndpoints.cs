using F5M04.Api.Seguranca;
using Microsoft.AspNetCore.Http.HttpResults;

namespace F5M04.Api.Catalogo;

public sealed record CriarProdutoRequest(string? Nome, decimal Preco);

public sealed record ProdutoResponse(Guid Id, string Nome, decimal Preco, bool Ativo)
{
    public static ProdutoResponse De(Produto p) => new(p.Id, p.Nome, p.Preco, p.Ativo);
}

public static class ProdutoEndpoints
{
    public static IEndpointRouteBuilder MapProdutoEndpoints(this IEndpointRouteBuilder app)
    {
        // Catálogo público: leitura anônima, declarada EXPLICITAMENTE (a FallbackPolicy exige login no resto).
        var publico = app.MapGroup("/produtos")
            .AllowAnonymous();
        publico.MapGet("/", (ICatalogo catalogo) =>
            TypedResults.Ok(catalogo.ListarAtivos().Select(ProdutoResponse.De).ToArray()));
        publico.MapGet("/{id:guid}", Results<Ok<ProdutoResponse>, NotFound> (Guid id, ICatalogo catalogo) =>
            catalogo.Obter(id) is { Ativo: true } p ? TypedResults.Ok(ProdutoResponse.De(p)) : TypedResults.NotFound());

        // Gestão do catálogo: só Admin. A política fica no GRUPO para nenhum endpoint novo "esquecer" dela.
        var gestao = app.MapGroup("/produtos")
            .RequireAuthorization(Politicas.Admin);
        gestao.MapPost("/", Criar);
        gestao.MapPost("/{id:guid}/desativar", Results<NoContent, NotFound> (Guid id, ICatalogo catalogo) =>
        {
            if (catalogo.Obter(id) is not { } produto) return TypedResults.NotFound();
            produto.Desativar();
            return TypedResults.NoContent();
        });

        return app;
    }

    private static Results<Created<ProdutoResponse>, ValidationProblem> Criar(CriarProdutoRequest request, ICatalogo catalogo)
    {
        var erros = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Nome)) erros["nome"] = ["Informe o nome."];
        if (request.Preco <= 0) erros["preco"] = ["O preço deve ser maior que zero."];
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var produto = new Produto(Guid.NewGuid(), request.Nome!.Trim(), request.Preco);
        catalogo.Adicionar(produto);
        return TypedResults.Created($"/produtos/{produto.Id}", ProdutoResponse.De(produto));
    }
}
