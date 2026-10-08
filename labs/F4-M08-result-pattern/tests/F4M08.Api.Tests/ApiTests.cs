using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using F4M08.Api.Http;
using F4M08.Api.Infra;
using F4M08.Api.Pedidos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace F4M08.Api.Tests;

/// <summary>
/// Passos 3 e 4: a API de verdade (WebApplicationFactory). Cada teste confere status,
/// content-type <c>application/problem+json</c> e o corpo ProblemDetails (title, status, detail, code, traceId, errors).
/// </summary>
public sealed class ApiTests : IAsyncDisposable
{
    private static readonly Guid Ana = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Bruno = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private readonly WebApplicationFactory<Program> _factory =
        new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddFakeLogging()));
    private readonly HttpClient _client;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ApiTests() => _client = _factory.CreateClient();

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<PedidoResponse> CriarAsync(Guid cliente)
    {
        var response = await _client.PostAsJsonAsync("/pedidos",
            new CriarPedidoRequest(cliente, [new ItemPedidoRequest(ProdutosConhecidos.Teclado.Id, 1)]), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PedidoResponse>(Ct))!;
    }

    /// <summary>Confere o "envelope" comum a todo erro e devolve o JSON para asserts específicos.</summary>
    private static async Task<JsonElement> LerProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        response.StatusCode.ShouldBe(status);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        json.GetProperty("status").GetInt32().ShouldBe((int)status);
        json.GetProperty("title").GetString().ShouldNotBeNullOrWhiteSpace();
        json.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        return json;
    }

    [Fact]
    public async Task Post_Valido_Retorna201ComLocationETotalDoServidor()
    {
        var response = await _client.PostAsJsonAsync("/pedidos", new CriarPedidoRequest(Ana,
        [
            new ItemPedidoRequest(ProdutosConhecidos.Teclado.Id, 2),
            new ItemPedidoRequest(ProdutosConhecidos.Mouse.Id, 1),
        ]), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var pedido = (await response.Content.ReadFromJsonAsync<PedidoResponse>(Ct))!;
        pedido.Total.ShouldBe(620.00m);
        response.Headers.Location!.ToString().ShouldBe($"/pedidos/{pedido.Id}");
    }

    [Fact]
    public async Task Post_Invalido_Retorna400ComErrorsPorCampoECode()
    {
        var response = await _client.PostAsJsonAsync("/pedidos", new CriarPedidoRequest(Guid.Empty, []), Ct);

        var json = await LerProblemAsync(response, HttpStatusCode.BadRequest);
        json.GetProperty("code").GetString().ShouldBe(PedidoErrors.CodigoValidacao);
        var errors = json.GetProperty("errors");
        errors.GetProperty("clienteId")[0].GetString().ShouldBe("Informe o cliente.");
        errors.GetProperty("itens")[0].GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Post_ProdutoInativo_Retorna422ComCodeEDetail()
    {
        var response = await _client.PostAsJsonAsync("/pedidos",
            new CriarPedidoRequest(Ana, [new ItemPedidoRequest(ProdutosConhecidos.Webcam.Id, 1)]), Ct);

        var json = await LerProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        json.GetProperty("code").GetString().ShouldBe(PedidoErrors.CodigoProdutoInativo);
        json.GetProperty("detail").GetString()!.ShouldContain("Webcam HD");
    }

    [Fact]
    public async Task Get_Inexistente_Retorna404ComCode()
    {
        var response = await _client.GetAsync($"/pedidos/{Guid.NewGuid()}", Ct);

        var json = await LerProblemAsync(response, HttpStatusCode.NotFound);
        json.GetProperty("code").GetString().ShouldBe(PedidoErrors.CodigoNaoEncontrado);
    }

    [Fact]
    public async Task Confirmar_DuasVezes_SegundaRetorna409()
    {
        var pedido = await CriarAsync(Ana);

        (await _client.PostAsync($"/pedidos/{pedido.Id}/confirmar", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var segunda = await _client.PostAsync($"/pedidos/{pedido.Id}/confirmar", null, Ct);

        var json = await LerProblemAsync(segunda, HttpStatusCode.Conflict);
        json.GetProperty("code").GetString().ShouldBe(PedidoErrors.CodigoTransicaoInvalida);
    }

    [Fact]
    public async Task Cancelar_PedidoDeOutroCliente_Retorna403_EDoDonoRetorna204()
    {
        var pedido = await CriarAsync(Ana);

        using var comoBruno = new HttpRequestMessage(HttpMethod.Post, $"/pedidos/{pedido.Id}/cancelar");
        comoBruno.Headers.Add(PedidoEndpoints.HeaderCliente, Bruno.ToString());
        var json = await LerProblemAsync(await _client.SendAsync(comoBruno, Ct), HttpStatusCode.Forbidden);
        json.GetProperty("code").GetString().ShouldBe(PedidoErrors.CodigoAcessoNegado);

        using var comoAna = new HttpRequestMessage(HttpMethod.Post, $"/pedidos/{pedido.Id}/cancelar");
        comoAna.Headers.Add(PedidoEndpoints.HeaderCliente, Ana.ToString());
        (await _client.SendAsync(comoAna, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancelar_SemHeaderObrigatorio_Retorna400ProblemDetails()
    {
        var pedido = await CriarAsync(Ana);

        var response = await _client.PostAsync($"/pedidos/{pedido.Id}/cancelar", null, Ct);

        var json = await LerProblemAsync(response, HttpStatusCode.BadRequest);
        json.GetProperty("code").GetString().ShouldBe(GlobalExceptionHandler.CodigoRequisicaoInvalida);
    }

    [Fact]
    public async Task ExcecaoInesperada_Retorna500GenericoSemVazarDetalhes_ELogaOErroCompleto()
    {
        var response = await _client.GetAsync("/diagnostico/falha", Ct);

        var corpo = await response.Content.ReadAsStringAsync(Ct);
        var json = await LerProblemAsync(response, HttpStatusCode.InternalServerError);
        json.GetProperty("code").GetString().ShouldBe(GlobalExceptionHandler.CodigoErroInterno);
        corpo.ShouldNotContain("SenhaSecreta123");
        corpo.ShouldNotContain("InvalidOperationException");
        corpo.ShouldNotContain("   at ");

        var logs = _factory.Services.GetRequiredService<FakeLogCollector>().GetSnapshot();
        var erro = logs.Where(r => r.Level == LogLevel.Error && r.Category == typeof(GlobalExceptionHandler).FullName).ShouldHaveSingleItem();
        erro.Exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldContain("SenhaSecreta123");
    }
}
