using System.Net;
using System.Net.Http.Json;
using F5M04.Api.Catalogo;
using F5M04.Api.Pedidos;
using F5M04.Api.Seguranca;
using F5M04.Api.Tests.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace F5M04.Api.Tests;

/// <summary>
/// Passo 2 — Autorização por políticas: o que você PODE fazer?
/// 401 = não sei quem você é; 403 = sei quem você é e você não pode.
/// </summary>
public sealed class PoliticasTests : IAsyncDisposable
{
    private readonly ApiFactory _api = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private static CriarPedidoRequest UmTeclado => new([new ItemPedidoRequest(ProdutosConhecidos.Teclado.Id, 1)]);

    [Fact]
    public async Task Catalogo_Anonimo_ConsegueListarEConsultar()
    {
        var anonimo = _api.Anonimo();

        var lista = await (await anonimo.GetAsync("/produtos", Ct)).LerAsync<ProdutoResponse[]>(HttpStatusCode.OK);
        lista.ShouldContain(p => p.Id == ProdutosConhecidos.Teclado.Id);
        (await anonimo.GetAsync($"/produtos/{ProdutosConhecidos.Mouse.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FallbackPolicy_EndpointSemPoliticaExplicita_ExigeUsuarioAutenticado()
    {
        // GET /pedidos não declara política nenhuma: quem protege é a FallbackPolicy.
        var anonimo = await _api.Anonimo().GetAsync("/pedidos", Ct);
        await anonimo.DeveSer401ComDesafioBearerAsync();

        (await _api.ComoCliente(ApiFactory.Ana).GetAsync("/pedidos", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Development", new[] { "GET /produtos/", "GET /produtos/{id:guid}", "POST /dev/token" })]
    [InlineData("Production", new[] { "GET /produtos/", "GET /produtos/{id:guid}" })]
    public async Task EndpointsAnonimos_SaoExatamenteOsDaListaPermitida(string ambiente, string[] esperados)
    {
        await using var api = new ApiFactory(ambiente);
        using var _ = api.CreateClient(); // sobe o host

        var anonimos = api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["*"])
                .Select(m => $"{m} {e.RoutePattern.RawText}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Um AllowAnonymous a mais é uma brecha; o emissor de tokens de dev NUNCA pode existir fora de Development.
        anonimos.ShouldBe(esperados.Order(StringComparer.Ordinal).ToArray());
    }

    [Theory]
    [InlineData("anonimo", HttpStatusCode.Unauthorized)]
    [InlineData("cliente", HttpStatusCode.Forbidden)]
    [InlineData("admin", HttpStatusCode.Created)]
    public async Task CriarProduto_SoAdmin(string quem, HttpStatusCode esperado)
    {
        var client = quem switch
        {
            "anonimo" => _api.Anonimo(),
            "cliente" => _api.ComoCliente(ApiFactory.Ana),
            _ => _api.ComoAdmin(),
        };

        var response = await client.PostAsJsonAsync("/produtos", new CriarProdutoRequest("Headset", 199.90m), Ct);

        if (esperado == HttpStatusCode.Created) response.StatusCode.ShouldBe(HttpStatusCode.Created);
        else await response.DeveSerProblemAsync(esperado);
    }

    [Fact]
    public async Task DesativarProduto_Cliente403_Admin204()
    {
        var url = $"/produtos/{ProdutosConhecidos.Mouse.Id}/desativar";

        await (await _api.ComoCliente(ApiFactory.Ana).PostAsync(url, null, Ct)).DeveSerProblemAsync(HttpStatusCode.Forbidden);
        (await _api.ComoAdmin().PostAsync(url, null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CriarPedido_Cliente_UsaOSubDoTokenEIgnoraClienteIdDoCorpo()
    {
        // Tentativa de criar pedido "em nome do Bruno" mandando clienteId no JSON (mass assignment / BOLA).
        var corpo = new { clienteId = ApiFactory.Bruno, itens = new[] { new { produtoId = ProdutosConhecidos.Teclado.Id, quantidade = 2 } } };

        var response = await _api.ComoCliente(ApiFactory.Ana).PostAsJsonAsync("/pedidos", corpo, Ct);

        var pedido = await response.LerAsync<PedidoResponse>(HttpStatusCode.Created);
        pedido.ClienteId.ShouldBe(ApiFactory.Ana);
        pedido.Total.ShouldBe(700.00m);
    }

    [Theory]
    [InlineData("pedidos.write", HttpStatusCode.Created)]
    [InlineData("pedidos.read pedidos.write", HttpStatusCode.Created)]
    [InlineData("pedidos.read", HttpStatusCode.Forbidden)]
    [InlineData("pedidos.writeall", HttpStatusCode.Forbidden)]
    [InlineData("PEDIDOS.WRITE", HttpStatusCode.Forbidden)]
    [InlineData("", HttpStatusCode.Forbidden)]
    public async Task CriarPedido_ExigeEscopoPedidosWrite(string escopos, HttpStatusCode esperado)
    {
        var response = await _api.ComoCliente(ApiFactory.Ana, escopos).PostAsJsonAsync("/pedidos", UmTeclado, Ct);

        if (esperado == HttpStatusCode.Created) response.StatusCode.ShouldBe(HttpStatusCode.Created);
        else await response.DeveSerProblemAsync(esperado);
    }

    [Fact]
    public async Task CriarPedido_AdminComEscopo_Retorna403PorqueNaoEhCliente()
    {
        var response = await _api.ComoAdmin().PostAsJsonAsync("/pedidos", UmTeclado, Ct);

        await response.DeveSerProblemAsync(HttpStatusCode.Forbidden);
    }
}
