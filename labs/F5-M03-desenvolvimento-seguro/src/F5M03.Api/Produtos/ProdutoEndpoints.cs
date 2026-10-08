using System.Net.Http.Headers;
using F5M03.Api.Dominio;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace F5M03.Api.Produtos;

public static class ProdutoEndpoints
{
    // Allowlist: valor que o cliente pode mandar → coluna real. Nada fora daqui chega ao "SQL".
    private static readonly Dictionary<string, string> ColunasDeOrdenacao = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nome"] = "Nome",
        ["preco"] = "Preco",
    };

    // Assinaturas ("magic numbers"): o Content-Type é declarado pelo cliente; os bytes não mentem tão fácil.
    private static readonly Dictionary<string, byte[]> Assinaturas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
    };

    public static IEndpointRouteBuilder MapProdutoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/produtos");
        grupo.MapGet("/", Listar);
        grupo.MapPost("/{id:guid}/imagem", EnviarImagem)
            // Defesa no servidor (Kestrel/IIS): o limite vale antes mesmo do handler rodar.
            .WithMetadata(new RequestSizeLimitAttribute(LimitesDeUpload.TamanhoMaximoEmBytes));
        return app;
    }

    internal static Results<Ok<ProdutoResponse[]>, ValidationProblem> Listar(string? ordenarPor, IProdutoRepositorio repositorio)
    {
        var coluna = "Nome";
        if (!string.IsNullOrWhiteSpace(ordenarPor) && !ColunasDeOrdenacao.TryGetValue(ordenarPor, out coluna!))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["ordenarPor"] = [$"Valores aceitos: {string.Join(", ", ColunasDeOrdenacao.Keys)}."],
            });
        }

        var produtos = repositorio.ListarAtivos(coluna);
        return TypedResults.Ok(produtos.Select(p => new ProdutoResponse(p.Id, p.Nome, p.Preco)).ToArray());
    }

    internal static async Task<Results<NoContent, NotFound, ProblemHttpResult>> EnviarImagem(
        Guid id, HttpRequest request, IProdutoRepositorio repositorio, CancellationToken ct)
    {
        if (repositorio.Obter(id) is not { } produto) return TypedResults.NotFound();

        // 1. Tipo declarado: allowlist.
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var tipo)
            || tipo.MediaType is null
            || !Assinaturas.TryGetValue(tipo.MediaType, out var assinatura))
        {
            return TipoNaoSuportado();
        }

        // 2. Tamanho declarado: barra cedo, sem ler nada.
        if (request.ContentLength > LimitesDeUpload.TamanhoMaximoEmBytes) return GrandeDemais();

        // 3. Tamanho real: Content-Length pode faltar (chunked) ou mentir. Leitura com teto.
        using var conteudo = new MemoryStream();
        var buffer = new byte[81_920];
        int lidos;
        while ((lidos = await request.Body.ReadAsync(buffer, ct)) > 0)
        {
            if (conteudo.Length + lidos > LimitesDeUpload.TamanhoMaximoEmBytes) return GrandeDemais();
            conteudo.Write(buffer, 0, lidos);
        }

        // 4. Conteúdo real: os primeiros bytes precisam bater com o tipo declarado.
        var bytes = conteudo.ToArray();
        if (!bytes.AsSpan().StartsWith(assinatura)) return TipoNaoSuportado();

        produto.Imagem = bytes;
        produto.TipoDaImagem = tipo.MediaType;
        return TypedResults.NoContent();
    }

    private static ProblemHttpResult GrandeDemais() => TypedResults.Problem(
        statusCode: StatusCodes.Status413PayloadTooLarge,
        title: "Arquivo grande demais",
        detail: $"O limite é {LimitesDeUpload.TamanhoMaximoEmBytes} bytes.");

    private static ProblemHttpResult TipoNaoSuportado() => TypedResults.Problem(
        statusCode: StatusCodes.Status415UnsupportedMediaType,
        title: "Tipo de arquivo não suportado",
        detail: $"Tipos aceitos: {string.Join(", ", LimitesDeUpload.TiposPermitidos)}.");
}
