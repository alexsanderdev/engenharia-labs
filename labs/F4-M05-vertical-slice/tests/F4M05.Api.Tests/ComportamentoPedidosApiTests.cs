using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace F4M05.Api.Tests;

/// <summary>
/// TESTES DE CARACTERIZAÇÃO: descrevem o comportamento HTTP da API (status, corpo, regras).
/// Passam no código inicial (camadas horizontais) e DEVEM continuar passando em cada passo da migração
/// para Vertical Slice. Só conversam com a API por HTTP — não conhecem nenhuma classe interna,
/// por isso sobrevivem a qualquer reorganização de pastas.
/// </summary>
public sealed class ComportamentoPedidosApiTests : IAsyncDisposable
{
    // Mesmos ids do catálogo semente da API.
    private static readonly Guid Teclado = Guid.Parse("11111111-1111-1111-1111-111111111111"); // 150,00
    private static readonly Guid Mouse = Guid.Parse("22222222-2222-2222-2222-222222222222");   // 80,50
    private static readonly Guid MonitorInativo = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _client;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ComportamentoPedidosApiTests() => _client = _factory.CreateClient();

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return _factory.DisposeAsync();
    }

    private static object NovoPedido(Guid clienteId, params (Guid ProdutoId, int Quantidade)[] itens) =>
        new { clienteId, itens = itens.Select(i => new { produtoId = i.ProdutoId, quantidade = i.Quantidade }).ToArray() };

    private async Task<JsonElement> CriarAsync(Guid? clienteId = null, params (Guid, int)[] itens)
    {
        var response = await _client.PostAsJsonAsync("/pedidos", NovoPedido(clienteId ?? Guid.NewGuid(), itens.Length == 0 ? [(Teclado, 1)] : itens), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    private static async Task DeveSerProblema(HttpResponseMessage response, HttpStatusCode status)
    {
        response.StatusCode.ShouldBe(status);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    // ---------- Criar ----------

    [Fact]
    public async Task Criar_PedidoValido_Retorna201ComTotalCalculadoNoServidor()
    {
        var response = await _client.PostAsJsonAsync("/pedidos", NovoPedido(Guid.NewGuid(), (Teclado, 2), (Mouse, 1)), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        var id = corpo.GetProperty("id").GetGuid();
        response.Headers.Location!.ToString().ShouldBe($"/pedidos/{id}");
        corpo.GetProperty("status").GetString().ShouldBe("Created");
        corpo.GetProperty("total").GetDecimal().ShouldBe(380.50m);
        corpo.GetProperty("itens").GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task Criar_SemItens_Retorna400ComErrosDeValidacao()
    {
        var response = await _client.PostAsJsonAsync("/pedidos", new { clienteId = Guid.NewGuid(), itens = Array.Empty<object>() }, Ct);

        await DeveSerProblema(response, HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        corpo.TryGetProperty("errors", out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Criar_QuantidadeNaoPositiva_Retorna400(int quantidade)
    {
        var response = await _client.PostAsJsonAsync("/pedidos", NovoPedido(Guid.NewGuid(), (Teclado, quantidade)), Ct);

        await DeveSerProblema(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_ClienteVazio_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/pedidos", NovoPedido(Guid.Empty, (Teclado, 1)), Ct);

        await DeveSerProblema(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_ProdutoInexistente_Retorna404()
    {
        var response = await _client.PostAsJsonAsync("/pedidos", NovoPedido(Guid.NewGuid(), (Guid.NewGuid(), 1)), Ct);

        await DeveSerProblema(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Criar_ProdutoInativo_Retorna422()
    {
        var response = await _client.PostAsJsonAsync("/pedidos", NovoPedido(Guid.NewGuid(), (Teclado, 1), (MonitorInativo, 1)), Ct);

        await DeveSerProblema(response, HttpStatusCode.UnprocessableEntity);
    }

    // ---------- Obter e listar ----------

    [Fact]
    public async Task Obter_PedidoExistente_Retorna200ComItens()
    {
        var criado = await CriarAsync(null, (Mouse, 3));
        var id = criado.GetProperty("id").GetGuid();

        var corpo = await _client.GetFromJsonAsync<JsonElement>($"/pedidos/{id}", Ct);

        corpo.GetProperty("id").GetGuid().ShouldBe(id);
        corpo.GetProperty("total").GetDecimal().ShouldBe(241.50m);
        corpo.GetProperty("itens")[0].GetProperty("quantidade").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task Obter_PedidoInexistente_Retorna404()
    {
        var response = await _client.GetAsync($"/pedidos/{Guid.NewGuid()}", Ct);

        await DeveSerProblema(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Listar_ComClienteId_RetornaSoOsPedidosDoCliente()
    {
        var cliente = Guid.NewGuid();
        var primeiro = (await CriarAsync(cliente)).GetProperty("id").GetGuid();
        var segundo = (await CriarAsync(cliente, (Mouse, 2))).GetProperty("id").GetGuid();
        await CriarAsync(Guid.NewGuid());

        var lista = await _client.GetFromJsonAsync<JsonElement>($"/pedidos?clienteId={cliente}", Ct);

        lista.EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ShouldBe([primeiro, segundo]);
        lista[1].GetProperty("total").GetDecimal().ShouldBe(161.00m);
        lista[1].GetProperty("status").GetString().ShouldBe("Created");
    }

    // ---------- Confirmar e cancelar ----------

    [Fact]
    public async Task Confirmar_PedidoCriado_Retorna200Confirmed()
    {
        var id = (await CriarAsync()).GetProperty("id").GetGuid();

        var response = await _client.PostAsync($"/pedidos/{id}/confirmar", null, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("status").GetString().ShouldBe("Confirmed");
    }

    [Fact]
    public async Task Confirmar_PedidoCancelado_Retorna409()
    {
        var id = (await CriarAsync()).GetProperty("id").GetGuid();
        (await _client.PostAsync($"/pedidos/{id}/cancelar", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await _client.PostAsync($"/pedidos/{id}/confirmar", null, Ct);

        await DeveSerProblema(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Confirmar_PedidoInexistente_Retorna404()
    {
        var response = await _client.PostAsync($"/pedidos/{Guid.NewGuid()}/confirmar", null, Ct);

        await DeveSerProblema(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancelar_PedidoCriado_Retorna200CancelledEPersiste()
    {
        var id = (await CriarAsync()).GetProperty("id").GetGuid();

        var response = await _client.PostAsync($"/pedidos/{id}/cancelar", null, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var corpo = await _client.GetFromJsonAsync<JsonElement>($"/pedidos/{id}", Ct);
        corpo.GetProperty("status").GetString().ShouldBe("Cancelled");
    }

    [Fact]
    public async Task Cancelar_PedidoConfirmado_Retorna409()
    {
        var id = (await CriarAsync()).GetProperty("id").GetGuid();
        (await _client.PostAsync($"/pedidos/{id}/confirmar", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await _client.PostAsync($"/pedidos/{id}/cancelar", null, Ct);

        await DeveSerProblema(response, HttpStatusCode.Conflict);
    }
}
