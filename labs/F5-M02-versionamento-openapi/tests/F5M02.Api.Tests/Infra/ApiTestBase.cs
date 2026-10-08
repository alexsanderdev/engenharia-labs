using System.Net;
using System.Text.Json;
using F5M02.Api.Dominio;
using Microsoft.AspNetCore.Mvc.Testing;

namespace F5M02.Api.Tests.Infra;

/// <summary>
/// Base dos testes de API: uma aplicação nova por teste (ambiente Development, padrão do WebApplicationFactory).
/// Os testes leem JSON "cru" porque verificam o contrato que o CLIENTE vê.
/// </summary>
public abstract class ApiTestBase : IAsyncDisposable
{
    protected static readonly Guid PedidoExemplo = PedidoRepositorio.PedidoExemploId;
    protected static readonly Guid ClienteExemplo = PedidoRepositorio.ClienteExemploId;

    protected ApiTestBase() : this(new WebApplicationFactory<Program>()) { }

    protected ApiTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = Factory.CreateClient();
    }

    protected WebApplicationFactory<Program> Factory { get; }
    protected HttpClient Client { get; }
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await Client.GetAsync(url, Ct);
        var corpo = await response.Content.ReadAsStringAsync(Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, corpo);
        return JsonDocument.Parse(corpo).RootElement.Clone();
    }

    /// <summary>Confere o envelope ProblemDetails (RFC 9457) comum a todo erro e devolve o JSON.</summary>
    protected static async Task<JsonElement> LerProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var corpo = await response.Content.ReadAsStringAsync(Ct);
        response.StatusCode.ShouldBe(status, corpo);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var json = JsonDocument.Parse(corpo).RootElement.Clone();
        json.GetProperty("status").GetInt32().ShouldBe((int)status);
        json.GetProperty("title").GetString().ShouldNotBeNullOrWhiteSpace();
        json.GetProperty("type").GetString().ShouldNotBeNullOrWhiteSpace();
        json.GetProperty("instance").GetString().ShouldNotBeNullOrWhiteSpace();
        json.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        json.GetProperty("code").GetString().ShouldBe(code);
        return json;
    }

    protected static string Header(HttpResponseMessage response, string nome) =>
        response.Headers.TryGetValues(nome, out var valores) ? string.Join(", ", valores) : "";
}
