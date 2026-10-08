using System.Net;
using System.Text.Json;
using F5M02.Api.Tests.Infra;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace F5M02.Api.Tests;

/// <summary>
/// Passo 4: o documento OpenAPI é o CONTRATO publicado. Os testes leem /openapi/v1.json e /openapi/v2.json
/// como um cliente (ou um gerador de código) faria.
/// </summary>
public sealed class OpenApiTests : ApiTestBase
{
    private static string[] Paths(JsonElement doc) => [.. doc.GetProperty("paths").EnumerateObject().Select(p => p.Name).Order()];

    private static string[] Respostas(JsonElement operacao) =>
        [.. operacao.GetProperty("responses").EnumerateObject().Select(r => r.Name).Order()];

    [Fact]
    public async Task DocumentoV1_SoTemOsPathsDaV1_ComAOperacaoMarcadaComoDepreciada()
    {
        var doc = await GetJsonAsync("/openapi/v1.json");

        Paths(doc).ShouldBe(["/v1/pedidos/{id}"]); // sem {version}, sem v2, sem /diagnostico
        var get = doc.GetProperty("paths").GetProperty("/v1/pedidos/{id}").GetProperty("get");
        get.GetProperty("deprecated").GetBoolean().ShouldBeTrue();
        get.GetProperty("operationId").GetString().ShouldBe("ObterPedidoV1");
        Respostas(get).ShouldBe(["200", "404"]);
        get.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString()).ShouldBe(["id"]);
    }

    [Fact]
    public async Task DocumentoV2_SoTemOsPathsDaV2_ComRespostasDeSucessoEErroDocumentadas()
    {
        var doc = await GetJsonAsync("/openapi/v2.json");

        Paths(doc).ShouldBe(["/v2/pedidos", "/v2/pedidos/{id}"]);
        var get = doc.GetProperty("paths").GetProperty("/v2/pedidos/{id}").GetProperty("get");
        (get.TryGetProperty("deprecated", out var deprecated) && deprecated.GetBoolean()).ShouldBeFalse();
        Respostas(get).ShouldBe(["200", "404"]);
        get.GetProperty("responses").GetProperty("404").GetProperty("content").TryGetProperty("application/problem+json", out _).ShouldBeTrue();
        get.GetProperty("summary").GetString().ShouldNotBeNullOrWhiteSpace();

        var post = doc.GetProperty("paths").GetProperty("/v2/pedidos").GetProperty("post");
        Respostas(post).ShouldBe(["201", "400", "422"]);
        post.GetProperty("responses").GetProperty("400").GetProperty("content").GetProperty("application/problem+json")
            .GetProperty("schema").GetProperty("$ref").GetString().ShouldBe("#/components/schemas/HttpValidationProblemDetails");
        post.GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString().ShouldBe("#/components/schemas/CriarPedidoV2Request");
    }

    [Fact]
    public async Task Documentos_TemTituloVersaoEDescricao_EAV1AvisaDepreciacaoESunset()
    {
        var v1 = (await GetJsonAsync("/openapi/v1.json")).GetProperty("info");
        var v2 = (await GetJsonAsync("/openapi/v2.json")).GetProperty("info");

        v1.GetProperty("title").GetString().ShouldBe("OrderFlow API");
        v2.GetProperty("title").GetString().ShouldBe("OrderFlow API");
        v1.GetProperty("version").GetString().ShouldBe("1.0");
        v2.GetProperty("version").GetString().ShouldBe("2.0");
        v1.GetProperty("description").GetString()!.ShouldContain("depreciada");
        v1.GetProperty("description").GetString()!.ShouldContain("2027-06-30");
        v2.GetProperty("description").GetString()!.ShouldNotContain("depreciada");
        v2.GetProperty("description").GetString()!.ShouldContain("RFC 9457");
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/openapi/v2.json")]
    public async Task Documentos_DeclaramESeguemOEsquemaDeSegurancaBearer(string url)
    {
        var doc = await GetJsonAsync(url);

        var bearer = doc.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        bearer.GetProperty("type").GetString().ShouldBe("http");
        bearer.GetProperty("scheme").GetString().ShouldBe("bearer");
        bearer.GetProperty("bearerFormat").GetString().ShouldBe("JWT");
        doc.GetProperty("security")[0].TryGetProperty("Bearer", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task DocumentoV2_SchemasDoContrato_ComDescricoesExemplosETiposHonestos()
    {
        var schemas = (await GetJsonAsync("/openapi/v2.json")).GetProperty("components").GetProperty("schemas");

        var pedido = schemas.GetProperty("PedidoV2Response").GetProperty("properties");
        pedido.GetProperty("valorTotal").GetProperty("$ref").GetString().ShouldBe("#/components/schemas/DinheiroResponse");
        pedido.GetProperty("status").GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
        pedido.TryGetProperty("total", out _).ShouldBeFalse();

        var dinheiro = schemas.GetProperty("DinheiroResponse");
        dinheiro.GetProperty("examples")[0].GetProperty("moeda").GetString().ShouldBe("BRL");
        dinheiro.GetProperty("properties").GetProperty("valor").GetProperty("type").GetString().ShouldBe("number");

        schemas.GetProperty("CriarPedidoV2Request").GetProperty("examples")[0].GetProperty("itens").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task DocumentoV1_NaoConheceOsSchemasDaV2()
    {
        var schemas = (await GetJsonAsync("/openapi/v1.json")).GetProperty("components").GetProperty("schemas");

        schemas.TryGetProperty("PedidoV1Response", out _).ShouldBeTrue();
        schemas.TryGetProperty("PedidoV2Response", out _).ShouldBeFalse();
        schemas.TryGetProperty("DinheiroResponse", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Scalar_EmDevelopment_ServeAUiApontandoParaODocumentoDaVersao()
    {
        var response = await Client.GetAsync("/scalar/v2", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("openapi/v2.json");
    }
}

/// <summary>Em produção: o documento (contrato) continua publicado, mas a UI interativa não.</summary>
public sealed class OpenApiProducaoTests()
    : ApiTestBase(new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Production")))
{
    [Fact]
    public async Task EmProducao_ScalarNaoEExposto_MasODocumentoSim()
    {
        (await Client.GetAsync("/scalar/v2", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await Client.GetAsync("/openapi/v2.json", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
