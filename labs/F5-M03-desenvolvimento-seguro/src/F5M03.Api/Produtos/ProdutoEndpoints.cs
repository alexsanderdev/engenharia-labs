using F5M03.Api.Dominio;

namespace F5M03.Api.Produtos;

// VULNERÁVEL — corrija (Passos 2, 3 e 8 do Lab).
public static class ProdutoEndpoints
{
    public static IEndpointRouteBuilder MapProdutoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/produtos");

        // VULNERÁVEL: "ordenarPor" vai direto para o ORDER BY (sem allowlist) e a entidade
        // inteira é devolvida (CustoInterno, bytes da imagem).
        // TODO: allowlist "nome" | "preco" (case-insensitive; vazio = nome); fora dela → 400
        //       ValidationProblem com o campo "ordenarPor". Devolva um contrato de saída.
        grupo.MapGet("/", (string? ordenarPor, IProdutoRepositorio repositorio) =>
            Results.Ok(repositorio.ListarAtivos(ordenarPor ?? "Nome")));

        // VULNERÁVEL: lê o corpo inteiro para a memória, sem limite e sem checar o tipo.
        // TODO: Content-Type fora de LimitesDeUpload.TiposPermitidos → 415;
        //       mais de LimitesDeUpload.TamanhoMaximoEmBytes → 413 (confira Content-Length E leia com teto);
        //       primeiros bytes diferentes da assinatura do tipo (PNG 89 50 4E 47 0D 0A 1A 0A, JPEG FF D8 FF) → 415.
        grupo.MapPost("/{id:guid}/imagem", async (Guid id, HttpRequest request, IProdutoRepositorio repositorio, CancellationToken ct) =>
        {
            if (repositorio.Obter(id) is not { } produto) return Results.NotFound();

            using var conteudo = new MemoryStream();
            await request.Body.CopyToAsync(conteudo, ct);
            produto.Imagem = conteudo.ToArray();
            produto.TipoDaImagem = request.ContentType;
            return Results.NoContent();
        });

        return app;
    }
}
