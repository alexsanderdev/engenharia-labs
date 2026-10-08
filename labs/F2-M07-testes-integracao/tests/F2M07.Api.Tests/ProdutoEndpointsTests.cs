using System.Net;
using System.Net.Http.Json;
using F2M07.Api.Produtos;
using F2M07.Api.Tests.Infra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace F2M07.Api.Tests;

/// <summary>Passo 5: o que só um banco de verdade pega (índice único).</summary>
[Collection(ColecaoIntegracao.Nome)]
public sealed class ProdutoEndpointsTests(ApiFixture fixture) : IntegracaoTestBase(fixture)
{
    [Fact]
    public async Task CriarProduto_Valido_Retorna201EPersisteNoBanco()
    {
        using var client = CriarCliente(ClienteA);

        var response = await client.PostAsJsonAsync("/produtos", new ProdutoRequest(" TEC-01 ", "Teclado", 199.90m), Ct);

        // 1. Status code
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        // 2. Payload
        var body = (await response.Content.ReadFromJsonAsync<ProdutoResponse>(Ct))!;
        body.Sku.ShouldBe("TEC-01");
        response.Headers.Location!.ToString().ShouldBe($"/produtos/{body.Id}");
        // 3. Efeito persistido (consultado com um DbContext NOVO, não o da requisição)
        var salvo = await ComBancoAsync(db => db.Produtos.AsNoTracking().SingleAsync(p => p.Id == body.Id, Ct));
        salvo.Sku.ShouldBe("TEC-01");
        salvo.Preco.ShouldBe(199.90m);
        salvo.Ativo.ShouldBeTrue();
    }

    [Fact]
    public async Task CriarProduto_SkuDuplicado_Retorna409ProblemDetailsENaoDuplica()
    {
        await SemearProdutoAsync("TEC-01", 100m);
        using var client = CriarCliente(ClienteA);

        var response = await client.PostAsJsonAsync("/produtos", new ProdutoRequest("TEC-01", "Outro teclado", 150m), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var problema = (await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!;
        problema.Title.ShouldBe("SKU já cadastrado");
        (await ComBancoAsync(db => db.Produtos.CountAsync(p => p.Sku == "TEC-01", Ct))).ShouldBe(1);
    }
}
