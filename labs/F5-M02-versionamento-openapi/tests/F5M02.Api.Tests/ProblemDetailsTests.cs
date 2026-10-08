using System.Net;
using F5M02.Api.Erros;
using F5M02.Api.Tests.Infra;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace F5M02.Api.Tests;

/// <summary>Passo 1: ProblemDetails padronizado (RFC 9457) em toda a API — inclusive no que não passa pelos seus endpoints.</summary>
public sealed class ProblemDetailsTests : ApiTestBase
{
    [Theory]
    [InlineData(400, "requisicao.invalida")]
    [InlineData(404, "recurso.nao_encontrado")]
    [InlineData(405, "metodo.nao_permitido")]
    [InlineData(415, "requisicao.media_type_nao_suportado")]
    [InlineData(500, "erro.interno")]
    [InlineData(503, "http.503")]
    public void CodigoPadrao_MapeiaStatusParaCodigoEstavel(int status, string esperado) =>
        ProblemDetailsPadrao.CodigoPadrao(status).ShouldBe(esperado);

    [Fact]
    public void Customizar_PreservaCodeExistente_EApontaTypeParaADocumentacaoDoCodigo()
    {
        var http = new DefaultHttpContext { TraceIdentifier = "trace-123" };
        http.Request.Path = "/v2/pedidos";
        var problema = new ProblemDetails { Status = 409, Extensions = { ["code"] = "pedido.transicao_invalida" } };

        ProblemDetailsPadrao.Customizar(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problema });

        problema.Extensions["code"].ShouldBe("pedido.transicao_invalida");
        problema.Extensions["traceId"].ShouldBe("trace-123");
        problema.Type.ShouldBe("https://docs.orderflow.dev/erros/pedido.transicao_invalida");
        problema.Instance.ShouldBe("/v2/pedidos");
    }

    [Fact]
    public void Customizar_CodeDeTerceiroSemPonto_MantemOTypeOriginal()
    {
        // Ex.: o Asp.Versioning usa code "UnsupportedApiVersion" e type próprio. Não sobrescreva o que é de terceiros.
        var problema = new ProblemDetails
        {
            Status = 400,
            Type = "https://docs.api-versioning.org/problems#unsupported",
            Extensions = { ["code"] = "UnsupportedApiVersion" },
        };

        ProblemDetailsPadrao.Customizar(new ProblemDetailsContext { HttpContext = new DefaultHttpContext(), ProblemDetails = problema });

        problema.Type.ShouldBe("https://docs.api-versioning.org/problems#unsupported");
        problema.Extensions["code"].ShouldBe("UnsupportedApiVersion");
    }

    [Fact]
    public async Task RotaInexistente_Retorna404ProblemDetailsComCodeTraceIdEInstance()
    {
        var response = await Client.GetAsync("/nao-existe", Ct);

        var json = await LerProblemAsync(response, HttpStatusCode.NotFound, "recurso.nao_encontrado");
        json.GetProperty("instance").GetString().ShouldBe("/nao-existe");
        json.GetProperty("type").GetString().ShouldBe("https://docs.orderflow.dev/erros/recurso.nao_encontrado");
    }

    [Fact]
    public async Task ExcecaoInesperada_Retorna500GenericoSemVazarDetalhes()
    {
        var response = await Client.GetAsync("/diagnostico/falha", Ct);

        var json = await LerProblemAsync(response, HttpStatusCode.InternalServerError, "erro.interno");
        json.GetProperty("detail").GetString()!.ShouldContain("traceId");
        var corpo = json.GetRawText();
        corpo.ShouldNotContain("SenhaSecreta123");
        corpo.ShouldNotContain("InvalidOperationException");
        corpo.ShouldNotContain(" at ");
    }
}
