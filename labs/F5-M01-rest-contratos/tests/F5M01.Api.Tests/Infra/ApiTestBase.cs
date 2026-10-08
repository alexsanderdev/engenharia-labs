using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using F5M01.Api.Dominio;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace F5M01.Api.Tests.Infra;

/// <summary>
/// Base dos testes de API: uma aplicação NOVA por teste (repositório em memória vazio) e relógio falso
/// para <c>criadoEm</c> determinístico. Os testes leem o JSON "cru" (<see cref="JsonElement"/>) de propósito:
/// testam o CONTRATO que o cliente vê, não os tipos C# do servidor.
/// </summary>
public abstract class ApiTestBase : IAsyncDisposable
{
    protected static readonly Guid Ana = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    protected static readonly Guid Bruno = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    protected FakeTimeProvider Relogio { get; } = new(new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero));
    private readonly WebApplicationFactory<Program> _factory;
    protected HttpClient Client { get; }
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected ApiTestBase()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<TimeProvider>(Relogio)));
        Client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected static object Item(Produto produto, int quantidade) => new { produtoId = produto.Id, quantidade };

    /// <summary>Cria um pedido via API (exige 201) e devolve o JSON + o ETag. Avança o relógio 1 minuto.</summary>
    protected async Task<(JsonElement Pedido, string ETag)> CriarPedidoAsync(Guid cliente, params object[] itens)
    {
        if (itens.Length == 0) itens = [Item(ProdutosConhecidos.Teclado, 1)];
        var response = await Client.PostAsJsonAsync("/pedidos", new { clienteId = cliente, itens }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        Relogio.Advance(TimeSpan.FromMinutes(1));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        return (json, response.Headers.ETag!.Tag);
    }

    protected static Guid IdDe(JsonElement pedido) => pedido.GetProperty("id").GetGuid();

    /// <summary>Confere o "envelope" ProblemDetails (RFC 9457) e devolve o JSON para asserts específicos.</summary>
    protected static async Task<JsonElement> LerProblemAsync(HttpResponseMessage response, HttpStatusCode status, string? code = null)
    {
        var corpo = await response.Content.ReadAsStringAsync(Ct);
        response.StatusCode.ShouldBe(status, corpo);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var json = JsonDocument.Parse(corpo).RootElement;
        json.GetProperty("status").GetInt32().ShouldBe((int)status);
        json.GetProperty("title").GetString().ShouldNotBeNullOrWhiteSpace();
        json.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        if (code is not null) json.GetProperty("code").GetString().ShouldBe(code);
        return json;
    }

    protected static HttpRequestMessage Requisicao(HttpMethod metodo, string url, object? corpo = null, string? ifMatch = null)
    {
        var request = new HttpRequestMessage(metodo, url);
        if (corpo is not null) request.Content = JsonContent.Create(corpo);
        if (ifMatch is not null) request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        return request;
    }
}
