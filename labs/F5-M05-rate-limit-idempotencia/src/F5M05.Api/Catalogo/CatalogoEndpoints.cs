using F5M05.Api.Autenticacao;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace F5M05.Api.Catalogo;

public static class CatalogoEndpoints
{
    /// <summary>Tag do output cache: invalidar a tag derruba TODAS as variações cacheadas do catálogo.</summary>
    public const string TagDoCatalogo = "catalogo";

    public sealed record AtualizarPrecoRequest(decimal Preco);

    public static IEndpointRouteBuilder MapCatalogoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/catalogo");

        // TODO (Passo 2): política PoliticasDeLimite.Catalogo (token bucket por IP).
        // TODO (Passo 6): output cache — expira em 5 min, varia pela query "categoria", tag TagDoCatalogo.
        grupo.MapGet("/", Listar);

        grupo.MapPut("/{id:guid}/preco", AtualizarPreco)
            .RequireAuthorization(p => p.RequireClaim(ApiKeyAuthenticationHandler.ClaimDoCliente, "backoffice"));

        return app;
    }

    internal static async Task<Ok<IReadOnlyList<Produto>>> Listar(string? categoria, ICatalogo catalogo, CancellationToken ct) =>
        TypedResults.Ok(await catalogo.ListarAsync(categoria, ct));

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> AtualizarPreco(
        Guid id, AtualizarPrecoRequest requisicao, ICatalogo catalogo, IOutputCacheStore cache, CancellationToken ct)
    {
        if (requisicao.Preco <= 0)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["Preco"] = ["Preço deve ser positivo."] });

        if (!await catalogo.AtualizarPrecoAsync(id, requisicao.Preco, ct)) return TypedResults.NotFound();

        // TODO (Passo 6): invalide o cache pela tag (cache.EvictByTagAsync).
        _ = cache;
        return TypedResults.NoContent();
    }
}
