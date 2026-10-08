using System.Net;
using System.Net.Http.Json;
using F5M04.Api.Catalogo;
using F5M04.Api.Pedidos;
using F5M04.Api.Tests.Infra;

namespace F5M04.Api.Tests;

/// <summary>
/// Passo 4 — Autorização baseada em recurso na API (BOLA/IDOR, OWASP API1:2023).
/// Quem não pode nem LER o pedido recebe 404 idêntico ao de um id inexistente; quem lê mas não pode agir recebe 403.
/// </summary>
public sealed class PedidoAutorizacaoTests : IAsyncDisposable
{
    private readonly ApiFactory _api = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    private async Task<PedidoResponse> CriarPedidoAsync(Guid cliente)
    {
        var response = await _api.ComoCliente(cliente).PostAsJsonAsync("/pedidos",
            new CriarPedidoRequest([new ItemPedidoRequest(ProdutosConhecidos.Teclado.Id, 1)]), Ct);
        return await response.LerAsync<PedidoResponse>(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ObterPedido_DonoEAdmin_Retornam200()
    {
        var pedido = await CriarPedidoAsync(ApiFactory.Ana);

        (await (await _api.ComoCliente(ApiFactory.Ana).GetAsync($"/pedidos/{pedido.Id}", Ct))
            .LerAsync<PedidoResponse>(HttpStatusCode.OK)).Id.ShouldBe(pedido.Id);
        (await (await _api.ComoAdmin().GetAsync($"/pedidos/{pedido.Id}", Ct))
            .LerAsync<PedidoResponse>(HttpStatusCode.OK)).ClienteId.ShouldBe(ApiFactory.Ana);
    }

    [Fact]
    public async Task ObterPedido_DeOutroCliente_Retorna404IndistinguivelDeInexistente()
    {
        var pedidoDaAna = await CriarPedidoAsync(ApiFactory.Ana);
        var bruno = _api.ComoCliente(ApiFactory.Bruno);

        // Ataque BOLA: Bruno está autenticado, tem papel e escopo certos, e troca o id na URL.
        var alheio = await bruno.GetAsync($"/pedidos/{pedidoDaAna.Id}", Ct);
        var inexistente = await bruno.GetAsync($"/pedidos/{Guid.NewGuid()}", Ct);

        var corpoAlheio = await alheio.DeveSerProblemAsync(HttpStatusCode.NotFound);
        var corpoInexistente = await inexistente.DeveSerProblemAsync(HttpStatusCode.NotFound);
        corpoAlheio.GetProperty("title").GetString().ShouldBe(corpoInexistente.GetProperty("title").GetString());
        var texto = corpoAlheio.GetRawText();
        texto.ShouldNotContain(ApiFactory.Ana.ToString(), customMessage: "Nada do pedido alheio pode vazar na resposta.");
        texto.ShouldNotContain("Teclado");
    }

    [Fact]
    public async Task CancelarPedido_DoDono_Retorna204()
    {
        var pedido = await CriarPedidoAsync(ApiFactory.Ana);
        var ana = _api.ComoCliente(ApiFactory.Ana);

        (await ana.PostAsync($"/pedidos/{pedido.Id}/cancelar", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var depois = await (await ana.GetAsync($"/pedidos/{pedido.Id}", Ct)).LerAsync<PedidoResponse>(HttpStatusCode.OK);
        depois.Status.ShouldBe(nameof(StatusPedido.Cancelled));
    }

    [Fact]
    public async Task CancelarPedido_DeOutroCliente_Retorna404ENaoAlteraOPedido()
    {
        var pedido = await CriarPedidoAsync(ApiFactory.Ana);

        var response = await _api.ComoCliente(ApiFactory.Bruno).PostAsync($"/pedidos/{pedido.Id}/cancelar", null, Ct);

        await response.DeveSerProblemAsync(HttpStatusCode.NotFound);
        var daAna = await (await _api.ComoCliente(ApiFactory.Ana).GetAsync($"/pedidos/{pedido.Id}", Ct))
            .LerAsync<PedidoResponse>(HttpStatusCode.OK);
        daAna.Status.ShouldBe(nameof(StatusPedido.Created));
    }

    [Fact]
    public async Task CancelarPedido_ComoAdmin_Retorna403PorquePodeLerMasNaoCancelar()
    {
        var pedido = await CriarPedidoAsync(ApiFactory.Ana);

        var response = await _api.ComoAdmin().PostAsync($"/pedidos/{pedido.Id}/cancelar", null, Ct);

        await response.DeveSerProblemAsync(HttpStatusCode.Forbidden);
    }
}
