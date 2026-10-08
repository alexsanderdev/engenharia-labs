namespace F5M05.Api.Relatorios;

public static class RelatorioEndpoints
{
    public static IEndpointRouteBuilder MapRelatorioEndpoints(this IEndpointRouteBuilder app)
    {
        // TODO (Passo 2): política PoliticasDeLimite.Relatorios (concorrência: no máximo N ao mesmo tempo).
        app.MapGet("/relatorios/vendas", async (IGeradorDeRelatorio gerador, CancellationToken ct) =>
                TypedResults.Ok(await gerador.GerarAsync(ct)))
            .RequireAuthorization();

        return app;
    }
}
