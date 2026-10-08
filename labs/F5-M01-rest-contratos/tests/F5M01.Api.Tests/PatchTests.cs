using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using F5M01.Api.Tests.Infra;

namespace F5M01.Api.Tests;

/// <summary>Passo 4 (na API): PATCH com JSON Merge Patch (RFC 7396) — 200, 415, 422 e 412.</summary>
public sealed class PatchTests : ApiTestBase
{
    private Task<HttpResponseMessage> PatchAsync(Guid id, string json, string contentType = "application/merge-patch+json", string? ifMatch = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/pedidos/{id}")
        {
            Content = new StringContent(json, Encoding.UTF8, contentType),
        };
        if (ifMatch is not null) request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        return Client.SendAsync(request, Ct);
    }

    [Fact]
    public async Task Patch_AtualizaSoOsCamposEnviados_MesclaObjetos_ENullRemove()
    {
        var (pedido, etagInicial) = await CriarPedidoAsync(Ana);
        var id = IdDe(pedido);

        var r1 = await PatchAsync(id, """
            { "observacao": "Entregar na portaria",
              "enderecoEntrega": { "logradouro": "Av. Paulista, 1000", "cidade": "São Paulo", "cep": "01310-100" } }
            """);
        r1.StatusCode.ShouldBe(HttpStatusCode.OK, await r1.Content.ReadAsStringAsync(Ct));
        r1.Headers.ETag!.Tag.ShouldNotBe(etagInicial);

        // Só o CEP: o resto do endereço e a observação ficam como estavam.
        var r2 = await PatchAsync(id, """{ "enderecoEntrega": { "cep": "01311-000" } }""");
        r2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var depois = await r2.Content.ReadFromJsonAsync<JsonElement>(Ct);
        depois.GetProperty("observacao").GetString().ShouldBe("Entregar na portaria");
        depois.GetProperty("enderecoEntrega").GetProperty("logradouro").GetString().ShouldBe("Av. Paulista, 1000");
        depois.GetProperty("enderecoEntrega").GetProperty("cep").GetString().ShouldBe("01311-000");

        // null remove.
        var r3 = await PatchAsync(id, """{ "observacao": null }""");
        r3.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r3.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("observacao").ValueKind.ShouldBe(JsonValueKind.Null);

        // E o GET confirma o estado final.
        var atual = await Client.GetFromJsonAsync<JsonElement>($"/pedidos/{id}", Ct);
        atual.GetProperty("enderecoEntrega").GetProperty("cidade").GetString().ShouldBe("São Paulo");
        atual.GetProperty("observacao").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Patch_ComContentTypeApplicationJson_Retorna415()
    {
        var (pedido, _) = await CriarPedidoAsync(Ana);

        var response = await PatchAsync(IdDe(pedido), """{ "observacao": "x" }""", "application/json");

        await LerProblemAsync(response, HttpStatusCode.UnsupportedMediaType, "requisicao.media_type_nao_suportado");
    }

    [Theory]
    [InlineData("""{ "status": "Completed" }""", "status")]
    [InlineData("""{ "total": 0.01 }""", "total")]
    [InlineData("""{ "enderecoEntrega": { "logradouro": "Rua A", "cidade": "Recife", "cep": "123" } }""", "enderecoEntrega.cep")]
    [InlineData("""{ "enderecoEntrega": { "cep": "50000-000" } }""", "enderecoEntrega.logradouro")]
    public async Task Patch_CampoNaoEditavelOuResultadoInvalido_Retorna422ComErrosPorCampo(string patch, string campo)
    {
        var (pedido, _) = await CriarPedidoAsync(Ana);

        var response = await PatchAsync(IdDe(pedido), patch);

        var problem = await LerProblemAsync(response, HttpStatusCode.UnprocessableEntity, "requisicao.validacao");
        problem.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue(problem.GetRawText());
    }

    [Fact]
    public async Task Patch_ComIfMatchDesatualizado_Retorna412_EIfMatchAtualFunciona()
    {
        var (pedido, etagVelho) = await CriarPedidoAsync(Ana);
        var id = IdDe(pedido);
        var r1 = await PatchAsync(id, """{ "observacao": "primeira" }""", ifMatch: etagVelho);
        r1.StatusCode.ShouldBe(HttpStatusCode.OK);

        var r2 = await PatchAsync(id, """{ "observacao": "segunda" }""", ifMatch: etagVelho);
        await LerProblemAsync(r2, HttpStatusCode.PreconditionFailed, "pedido.versao_desatualizada");

        var r3 = await PatchAsync(id, """{ "observacao": "segunda" }""", ifMatch: r1.Headers.ETag!.Tag);
        r3.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
