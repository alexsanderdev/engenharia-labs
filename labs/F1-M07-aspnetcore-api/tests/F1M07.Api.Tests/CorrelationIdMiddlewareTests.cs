using System.Net;
using F1M07.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace F1M07.Api.Tests;

/// <summary>Passo 6: middleware customizado e ordem do pipeline.</summary>
public sealed class CorrelationIdMiddlewareTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private const string Header = CorrelationIdMiddleware.HeaderName;

    [Fact]
    public async Task Request_SemHeader_RespostaTrazCorrelationIdGerado()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/products", Ct);

        response.Headers.TryGetValues(Header, out var values).ShouldBeTrue();
        var id = values!.Single();
        Guid.TryParseExact(id, "N", out _).ShouldBeTrue($"esperado Guid no formato N, veio '{id}'");
    }

    [Fact]
    public async Task Request_ComHeader_RespostaEcoaOMesmoValor()
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/products");
        request.Headers.Add(Header, "pedido-42");

        var response = await client.SendAsync(request, Ct);

        response.Headers.GetValues(Header).Single().ShouldBe("pedido-42");
    }

    [Fact]
    public async Task Request_DuasChamadasSemHeader_GeramIdsDiferentes()
    {
        var client = factory.CreateClient();

        var a = (await client.GetAsync("/products", Ct)).Headers.GetValues(Header).Single();
        var b = (await client.GetAsync("/products", Ct)).Headers.GetValues(Header).Single();

        a.ShouldNotBe(b);
    }

    [Fact]
    public async Task RotaInexistente_404AindaTrazCorrelationId()
    {
        // Prova que o middleware está no início do pipeline, e não como filtro de endpoint.
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/nao-existe");
        request.Headers.Add(Header, "rastreio-404");

        var response = await client.SendAsync(request, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.GetValues(Header).Single().ShouldBe("rastreio-404");
    }
}
