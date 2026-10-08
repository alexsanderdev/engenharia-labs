using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using F5M02.Api.Dominio;
using F5M02.Api.Tests.Infra;

namespace F5M02.Api.Tests;

/// <summary>Passos 2 e 3: v1 (depreciada) e v2 convivendo, roteamento por segmento de URL e headers de versão.</summary>
public sealed class VersionamentoTests : ApiTestBase
{
    [Fact]
    public async Task V1_RetornaContratoLegado_StatusNumericoETotalSemMoeda()
    {
        var json = await GetJsonAsync($"/v1/pedidos/{PedidoExemplo}");

        json.EnumerateObject().Select(p => p.Name).Order().ShouldBe(["clienteId", "id", "quantidadeItens", "status", "total"]);
        json.GetProperty("status").GetInt32().ShouldBe(1); // Confirmed
        json.GetProperty("total").GetDecimal().ShouldBe(620.00m);
    }

    [Fact]
    public async Task V2_RetornaContratoNovo_StatusTextualEValorTotalComMoeda()
    {
        var json = await GetJsonAsync($"/v2/pedidos/{PedidoExemplo}");

        json.TryGetProperty("total", out _).ShouldBeFalse(); // a mudança incompatível: "total" não existe mais
        json.GetProperty("status").GetString().ShouldBe("Confirmed");
        json.GetProperty("valorTotal").GetProperty("valor").GetDecimal().ShouldBe(620.00m);
        json.GetProperty("valorTotal").GetProperty("moeda").GetString().ShouldBe("BRL");
        json.GetProperty("itens").GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task V2_InformaVersoesSuportadasEDepreciadas_ESemHeadersDeSunset()
    {
        var response = await Client.GetAsync($"/v2/pedidos/{PedidoExemplo}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Header(response, "api-supported-versions").ShouldBe("2.0");
        Header(response, "api-deprecated-versions").ShouldBe("1.0");
        response.Headers.Contains("Sunset").ShouldBeFalse();
        response.Headers.Contains("Deprecation").ShouldBeFalse();
    }

    [Fact]
    public async Task V1_AnunciaDepreciacaoESunset_ComLinkParaAPolitica()
    {
        var response = await Client.GetAsync($"/v1/pedidos/{PedidoExemplo}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Header(response, "api-deprecated-versions").ShouldBe("1.0");
        // RFC 9745: Deprecation é uma data estruturada (@ + segundos Unix). 2026-10-01T00:00:00Z.
        Header(response, "Deprecation").ShouldBe($"@{new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()}");
        // RFC 8594: Sunset é uma HTTP-date. 2027-06-30T00:00:00Z.
        DateTimeOffset.Parse(Header(response, "Sunset"), System.Globalization.CultureInfo.InvariantCulture)
            .ShouldBe(new DateTimeOffset(2027, 6, 30, 0, 0, 0, TimeSpan.Zero));
        var link = Header(response, "Link");
        link.ShouldContain("rel=\"sunset\"");
        link.ShouldContain("rel=\"deprecation\"");
        link.ShouldContain("https://docs.orderflow.dev/api/politica-de-versoes#v1");
    }

    [Theory]
    [InlineData("/v3/pedidos/0f0f0f0f-0000-0000-0000-000000000001")]
    [InlineData("/pedidos/0f0f0f0f-0000-0000-0000-000000000001")]
    public async Task VersaoInexistenteOuAusenteNaUrl_Retorna404ProblemDetails(string url)
    {
        // Com versão no segmento da URL, a versão faz parte da identidade do recurso: /v3/pedidos/... não existe → 404.
        var response = await Client.GetAsync(url, Ct);

        await LerProblemAsync(response, HttpStatusCode.NotFound, "recurso.nao_encontrado");
    }

    [Fact]
    public async Task V2_PedidoInexistente_Retorna404ComCodeDeNegocio_EHeadersDeVersao()
    {
        var response = await Client.GetAsync($"/v2/pedidos/{Guid.NewGuid()}", Ct);

        await LerProblemAsync(response, HttpStatusCode.NotFound, "pedido.nao_encontrado");
        Header(response, "api-supported-versions").ShouldBe("2.0");
    }

    [Fact]
    public async Task V2_PostValido_Retorna201ComLocationNaV2()
    {
        var response = await Client.PostAsJsonAsync("/v2/pedidos", new
        {
            clienteId = ClienteExemplo,
            itens = new[] { new { produtoId = ProdutosConhecidos.Mouse.Id, quantidade = 3 } },
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        response.Headers.Location!.ToString().ShouldBe($"/v2/pedidos/{json.GetProperty("id").GetGuid()}");
        json.GetProperty("valorTotal").GetProperty("valor").GetDecimal().ShouldBe(360.00m);
        (await Client.GetAsync(response.Headers.Location, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_PostInvalido_Retorna400ValidationProblemComErrosPorCampo()
    {
        var response = await Client.PostAsJsonAsync("/v2/pedidos", new
        {
            clienteId = Guid.Empty,
            itens = new[] { new { produtoId = ProdutosConhecidos.Mouse.Id, quantidade = 0 } },
        }, Ct);

        var json = await LerProblemAsync(response, HttpStatusCode.BadRequest, "pedido.validacao");
        json.GetProperty("type").GetString().ShouldBe("https://docs.orderflow.dev/erros/pedido.validacao");
        var errors = json.GetProperty("errors");
        errors.GetProperty("clienteId")[0].GetString().ShouldBe("Informe o cliente.");
        errors.TryGetProperty("itens[0].quantidade", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task V2_PostComProdutoInativo_Retorna422()
    {
        var response = await Client.PostAsJsonAsync("/v2/pedidos", new
        {
            clienteId = ClienteExemplo,
            itens = new[] { new { produtoId = ProdutosConhecidos.Webcam.Id, quantidade = 1 } },
        }, Ct);

        await LerProblemAsync(response, HttpStatusCode.UnprocessableEntity, "produto.indisponivel");
    }
}
