using System.Net;
using System.Net.Http.Json;
using F2M07.Api.Pedidos;
using F2M07.Api.Tests.Infra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace F2M07.Api.Tests;

/// <summary>
/// Passo 6: o fluxo crítico "criar pedido" de ponta a ponta. Cada teste verifica
/// status code, payload E o efeito persistido no banco.
/// </summary>
[Collection(ColecaoIntegracao.Nome)]
public sealed class PedidoEndpointsTests(ApiFixture fixture) : IntegracaoTestBase(fixture)
{
    private static CriarPedidoRequest Pedido(params (Guid ProdutoId, int Quantidade)[] itens) =>
        new([.. itens.Select(i => new ItemPedidoRequest(i.ProdutoId, i.Quantidade))]);

    private Task<int> ContarPedidosAsync() => ComBancoAsync(db => db.Pedidos.CountAsync(Ct));

    // ---------- Caminho feliz ----------

    [Fact]
    public async Task CriarPedido_Valido_Retorna201ComLocationETotalCalculadoNoServidor()
    {
        var teclado = await SemearProdutoAsync("TEC-01", 199.90m);
        var mouse = await SemearProdutoAsync("MOU-01", 50.05m);
        using var client = CriarCliente(ClienteA);

        var response = await client.PostAsJsonAsync("/pedidos", Pedido((teclado.Id, 2), (mouse.Id, 3)), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = (await response.Content.ReadFromJsonAsync<PedidoResponse>(Ct))!;
        response.Headers.Location!.ToString().ShouldBe($"/pedidos/{body.Id}");
        body.Status.ShouldBe("Created");
        body.Total.ShouldBe(549.95m); // 2 × 199,90 + 3 × 50,05
        body.Itens.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CriarPedido_Valido_PersistePedidoComItensClienteEDataDoRelogio()
    {
        var teclado = await SemearProdutoAsync("TEC-01", 199.90m);
        using var client = CriarCliente(ClienteA);

        var response = await client.PostAsJsonAsync("/pedidos", Pedido((teclado.Id, 2)), Ct);
        var body = (await response.Content.ReadFromJsonAsync<PedidoResponse>(Ct))!;

        var salvo = await ComBancoAsync(db => db.Pedidos.AsNoTracking().SingleAsync(p => p.Id == body.Id, Ct));
        salvo.ClienteId.ShouldBe(ClienteA);
        salvo.Status.ShouldBe(StatusPedido.Created);
        salvo.Total.ShouldBe(399.80m);
        salvo.CriadoEm.ShouldBe(Fixture.Relogio.GetUtcNow());
        var item = salvo.Itens.ShouldHaveSingleItem();
        item.ProdutoId.ShouldBe(teclado.Id);
        item.Quantidade.ShouldBe(2);
        item.PrecoUnitario.ShouldBe(199.90m);
    }

    [Fact]
    public async Task CriarPedido_PrecoDoProdutoMudaDepois_PedidoMantemPrecoDaCompra()
    {
        var teclado = await SemearProdutoAsync("TEC-01", 100m);
        using var client = CriarCliente(ClienteA);
        var criado = await client.PostAsJsonAsync("/pedidos", Pedido((teclado.Id, 1)), Ct);
        var pedido = (await criado.Content.ReadFromJsonAsync<PedidoResponse>(Ct))!;

        await ComBancoAsync(async db =>
        {
            var produto = await db.Produtos.SingleAsync(p => p.Id == teclado.Id, Ct);
            produto.AlterarPreco(500m);
            await db.SaveChangesAsync(Ct);
        });

        var lido = (await client.GetFromJsonAsync<PedidoResponse>($"/pedidos/{pedido.Id}", Ct))!;
        lido.Total.ShouldBe(100m);
        lido.Itens.ShouldHaveSingleItem().PrecoUnitario.ShouldBe(100m);
    }

    // ---------- Entrada inválida (400) ----------

    [Fact]
    public async Task CriarPedido_SemItens_Retorna400ENaoPersisteNada()
    {
        using var client = CriarCliente(ClienteA);

        var response = await client.PostAsJsonAsync("/pedidos", Pedido(), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problema = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct))!;
        problema.Errors.ShouldContainKey("Itens");
        (await ContarPedidosAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task CriarPedido_QuantidadeZero_Retorna400ComOCampoDoItem()
    {
        var teclado = await SemearProdutoAsync("TEC-01", 100m);
        using var client = CriarCliente(ClienteA);

        var response = await client.PostAsJsonAsync("/pedidos", Pedido((teclado.Id, 1), (teclado.Id, 0)), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problema = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct))!;
        problema.Errors.ShouldContainKey("Itens[1].Quantidade");
        (await ContarPedidosAsync()).ShouldBe(0);
    }

    // ---------- Regras que dependem do banco (422) ----------

    [Fact]
    public async Task CriarPedido_ProdutoInativo_Retorna422ENaoPersisteNada()
    {
        var ativo = await SemearProdutoAsync("TEC-01", 100m);
        var inativo = await SemearProdutoAsync("TEC-OLD", 80m, ativo: false);
        using var client = CriarCliente(ClienteA);

        var response = await client.PostAsJsonAsync("/pedidos", Pedido((ativo.Id, 1), (inativo.Id, 1)), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problema = (await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!;
        problema.Detail.ShouldNotBeNull().ShouldContain(inativo.Id.ToString());
        (await ContarPedidosAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task CriarPedido_ProdutoInexistente_Retorna422()
    {
        using var client = CriarCliente(ClienteA);
        var fantasma = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/pedidos", Pedido((fantasma, 1)), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ContarPedidosAsync()).ShouldBe(0);
    }

    // ---------- Leitura com isolamento por cliente ----------

    [Fact]
    public async Task ObterPedido_DeOutroCliente_Retorna404()
    {
        var teclado = await SemearProdutoAsync("TEC-01", 100m);
        var pedidoDoB = await SemearPedidoAsync(ClienteB, teclado, StatusPedido.Created);
        using var clientA = CriarCliente(ClienteA);

        var response = await clientA.GetAsync($"/pedidos/{pedidoDoB.Id}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---------- Cancelamento ----------

    [Fact]
    public async Task CancelarPedido_Created_Retorna204EGravaCancelledNoBanco()
    {
        var teclado = await SemearProdutoAsync("TEC-01", 100m);
        var pedido = await SemearPedidoAsync(ClienteA, teclado, StatusPedido.Created);
        using var client = CriarCliente(ClienteA);

        var response = await client.PostAsync($"/pedidos/{pedido.Id}/cancelar", null, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var salvo = await ComBancoAsync(db => db.Pedidos.AsNoTracking().SingleAsync(p => p.Id == pedido.Id, Ct));
        salvo.Status.ShouldBe(StatusPedido.Cancelled);
    }

    [Fact]
    public async Task CancelarPedido_Completed_Retorna409EStatusNaoMuda()
    {
        var teclado = await SemearProdutoAsync("TEC-01", 100m);
        var pedido = await SemearPedidoAsync(ClienteA, teclado, StatusPedido.Completed);
        using var client = CriarCliente(ClienteA);

        var response = await client.PostAsync($"/pedidos/{pedido.Id}/cancelar", null, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var salvo = await ComBancoAsync(db => db.Pedidos.AsNoTracking().SingleAsync(p => p.Id == pedido.Id, Ct));
        salvo.Status.ShouldBe(StatusPedido.Completed);
    }
}
