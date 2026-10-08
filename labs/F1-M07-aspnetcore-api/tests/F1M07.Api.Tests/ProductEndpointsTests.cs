using System.Net;
using System.Net.Http.Json;
using F1M07.Api.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace F1M07.Api.Tests;

/// <summary>
/// Passos 2 a 5: a API de verdade, em memória, via WebApplicationFactory.
/// Cada teste cria sua própria factory (e portanto seu próprio repositório em memória),
/// para que um teste não enxergue os produtos criados por outro.
/// </summary>
public sealed class ProductEndpointsTests : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _client;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ProductEndpointsTests() => _client = _factory.CreateClient();

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return _factory.DisposeAsync();
    }

    private async Task<ProductResponse> CreateAsync(string name = "Teclado", decimal price = 199.90m)
    {
        var response = await _client.PostAsJsonAsync("/products", new ProductRequest(name, price), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>(Ct))!;
    }

    // ---------- Passo 2: leitura ----------

    [Fact]
    public async Task GetAll_SemProdutos_Retorna200ComListaVazia()
    {
        var response = await _client.GetAsync("/products", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProductResponse[]>(Ct);
        body.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetById_Inexistente_Retorna404()
    {
        var response = await _client.GetAsync($"/products/{Guid.NewGuid()}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---------- Passo 3: criação com TypedResults ----------

    [Fact]
    public async Task Create_ComDadosValidos_Retorna201ComLocationECorpo()
    {
        var response = await _client.PostAsJsonAsync("/products", new ProductRequest("  Mouse gamer ", 149.90m), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = (await response.Content.ReadFromJsonAsync<ProductResponse>(Ct))!;
        body.Id.ShouldNotBe(Guid.Empty);
        body.Name.ShouldBe("Mouse gamer");
        body.Price.ShouldBe(149.90m);
        body.IsActive.ShouldBeTrue();
        response.Headers.Location!.ToString().ShouldBe($"/products/{body.Id}");
    }

    [Fact]
    public async Task Create_DepoisGetPelaLocation_RetornaOMesmoProduto()
    {
        var created = await _client.PostAsJsonAsync("/products", new ProductRequest("Monitor", 999m), Ct);

        var fetched = await _client.GetFromJsonAsync<ProductResponse>(created.Headers.Location, Ct);

        fetched.ShouldNotBeNull();
        fetched.Name.ShouldBe("Monitor");
    }

    [Fact]
    public async Task GetAll_ComProdutos_RetornaOrdenadoPorNome()
    {
        await CreateAsync("Webcam");
        await CreateAsync("Cabo HDMI");

        var all = await _client.GetFromJsonAsync<ProductResponse[]>("/products", Ct);

        all!.Select(p => p.Name).ShouldBe(["Cabo HDMI", "Webcam"]);
    }

    // ---------- Passo 4: validação com endpoint filter ----------

    [Fact]
    public async Task Create_Invalido_Retorna400ValidationProblemComOsCampos()
    {
        var response = await _client.PostAsJsonAsync("/products", new ProductRequest("", 0m), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Ct))!;
        problem.Errors.Keys.ShouldBe(["Name", "Price"], ignoreOrder: true);
    }

    [Fact]
    public async Task Create_Invalido_NaoPersisteNada()
    {
        await _client.PostAsJsonAsync("/products", new ProductRequest(null, 10m), Ct);

        var all = await _client.GetFromJsonAsync<ProductResponse[]>("/products", Ct);
        all.ShouldBeEmpty();
    }

    // ---------- Passo 5: alteração, ativação e remoção ----------

    [Fact]
    public async Task Update_Existente_Retorna204EAlteraOsDados()
    {
        var product = await CreateAsync();

        var response = await _client.PutAsJsonAsync($"/products/{product.Id}", new ProductRequest("Teclado ABNT2", 229.90m), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var fetched = await _client.GetFromJsonAsync<ProductResponse>($"/products/{product.Id}", Ct);
        fetched!.Name.ShouldBe("Teclado ABNT2");
        fetched.Price.ShouldBe(229.90m);
    }

    [Fact]
    public async Task Update_Inexistente_Retorna404()
    {
        var response = await _client.PutAsJsonAsync($"/products/{Guid.NewGuid()}", new ProductRequest("X", 1m), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Invalido_Retorna400MesmoSeOProdutoExiste()
    {
        var product = await CreateAsync();

        var response = await _client.PutAsJsonAsync($"/products/{product.Id}", new ProductRequest("Ok", -5m), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeactivateEActivate_AlternamIsActive()
    {
        var product = await CreateAsync();

        (await _client.PostAsync($"/products/{product.Id}/deactivate", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _client.GetFromJsonAsync<ProductResponse>($"/products/{product.Id}", Ct))!.IsActive.ShouldBeFalse();

        (await _client.PostAsync($"/products/{product.Id}/activate", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _client.GetFromJsonAsync<ProductResponse>($"/products/{product.Id}", Ct))!.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Deactivate_Inexistente_Retorna404()
    {
        var response = await _client.PostAsync($"/products/{Guid.NewGuid()}/deactivate", null, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Existente_Retorna204EDepoisGetRetorna404()
    {
        var product = await CreateAsync();

        (await _client.DeleteAsync($"/products/{product.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/products/{product.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await _client.DeleteAsync($"/products/{product.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
