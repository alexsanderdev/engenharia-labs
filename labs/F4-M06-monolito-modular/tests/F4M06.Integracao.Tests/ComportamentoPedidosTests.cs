using System.Net;
using System.Net.Http.Json;
using F4M06.Integracao.Tests.Infra;
using Microsoft.AspNetCore.Mvc;

namespace F4M06.Integracao.Tests;

/// <summary>
/// Testes de CARACTERIZAÇÃO: o comportamento que o OrderFlow já tem e que a refatoração das fronteiras
/// NÃO pode quebrar. Passam desde o início e têm que continuar verdes o tempo todo.
/// </summary>
[Collection(ColecaoIntegracao.Nome)]
public sealed class ComportamentoPedidosTests(OrderFlowFixture fixture) : IntegracaoTestBase(fixture)
{
    [Fact]
    public async Task CriarPedido_CalculaTotalNoServidorComPrecoDoCatalogo()
    {
        var cliente = await CriarClienteAsync();
        var cafe = await CriarProdutoAsync("Café 500g", 32.50m);
        var caneca = await CriarProdutoAsync("Caneca", 45m);

        var pedido = await CriarPedidoAsync(cliente.Id, (cafe.Id, 2), (caneca.Id, 1));

        pedido.Status.ShouldBe("Created");
        pedido.Total.ShouldBe(110m);
        pedido.Itens.ShouldContain(i => i.NomeProduto == "Café 500g" && i.PrecoUnitario == 32.50m && i.Quantidade == 2);
    }

    [Fact]
    public async Task CriarPedido_ProdutoInativo_Retorna422()
    {
        var cliente = await CriarClienteAsync();
        var produto = await CriarProdutoAsync("Chá descontinuado", 20m);
        await DesativarProdutoAsync(produto.Id);

        var resposta = await PostarPedidoAsync(cliente.Id, (produto.Id, 1));

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        problema!.Detail.ShouldNotBeNull().ShouldContain("inativo");
    }

    [Fact]
    public async Task CriarPedido_ClienteInexistente_Retorna422()
    {
        var produto = await CriarProdutoAsync("Café 500g", 32.50m);

        var resposta = await PostarPedidoAsync(Guid.NewGuid(), (produto.Id, 1));

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CriarPedido_GuardaPrecoDoMomento_AlterarPrecoNoCatalogoNaoMudaOPedido()
    {
        var cliente = await CriarClienteAsync();
        var produto = await CriarProdutoAsync("Café 500g", 30m);
        var pedido = await CriarPedidoAsync(cliente.Id, (produto.Id, 3));

        await AlterarPrecoAsync(produto.Id, 99m);

        var lido = await Api.GetFromJsonAsync<PedidoDto>($"/pedidos/{pedido.Id}", Ct);
        lido!.Total.ShouldBe(90m);
        lido.Itens.Single().PrecoUnitario.ShouldBe(30m);
    }

    [Fact]
    public async Task ConfirmarPedido_DuasVezes_SegundaRetorna409()
    {
        var cliente = await CriarClienteAsync();
        var produto = await CriarProdutoAsync("Café 500g", 30m);
        var pedido = await CriarPedidoAsync(cliente.Id, (produto.Id, 1));

        (await ConfirmarPedidoAsync(pedido.Id)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ConfirmarPedidoAsync(pedido.Id)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PedidoCriadoMasNaoConfirmado_NaoAlteraOCliente()
    {
        var cliente = await CriarClienteAsync();
        var produto = await CriarProdutoAsync("Café 500g", 30m);
        await CriarPedidoAsync(cliente.Id, (produto.Id, 10));

        var lido = await ObterClienteAsync(cliente.Id);

        lido.TotalGasto.ShouldBe(0m);
        lido.PedidosConfirmados.ShouldBe(0);
        lido.Nivel.ShouldBe("Bronze");
    }
}
