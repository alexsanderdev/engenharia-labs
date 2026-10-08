using F5M05.Api.Autenticacao;
using F5M05.Api.RateLimiting;
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

        grupo.MapGet("/", Listar)
            .RequireRateLimiting(PoliticasDeLimite.Catalogo)
            // Output cache no servidor: a segunda leitura não chega ao handler (nem ao banco).
            // Varia pela categoria; expira sozinho em 5 min; e é invalidado pela tag ao mudar preço.
            .CacheOutput(politica => politica
                .Expire(TimeSpan.FromMinutes(5))
                .SetVaryByQuery("categoria")
                .Tag(TagDoCatalogo));

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

        // Escreveu → invalida. Sem isso o cliente vê preço velho até o cache expirar.
        await cache.EvictByTagAsync(TagDoCatalogo, ct);
        return TypedResults.NoContent();
    }
}
