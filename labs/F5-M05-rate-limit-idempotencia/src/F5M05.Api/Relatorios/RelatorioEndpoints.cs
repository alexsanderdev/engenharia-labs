using F5M05.Api.RateLimiting;

namespace F5M05.Api.Relatorios;

public static class RelatorioEndpoints
{
    public static IEndpointRouteBuilder MapRelatorioEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/relatorios/vendas", async (IGeradorDeRelatorio gerador, CancellationToken ct) =>
                TypedResults.Ok(await gerador.GerarAsync(ct)))
            .RequireAuthorization()
            .RequireRateLimiting(PoliticasDeLimite.Relatorios); // concorrência: no máximo N ao mesmo tempo

        return app;
    }
}
