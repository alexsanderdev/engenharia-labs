using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using F5M01.Api.Dominio;
using F5M01.Api.Tests.Infra;

namespace F5M01.Api.Tests;

/// <summary>Passos 1 e 3: recursos, sub-recursos, métodos, status codes e contrato (DTO ≠ entidade).</summary>
public sealed class RecursosEStatusTests : ApiTestBase
{
    [Fact]
    public async Task RpcLegado_NaoEstaMaisExposto()
    {
        var response = await Client.PostAsJsonAsync("/api/CriarPedido",
            new { clienteId = Ana, itens = new[] { Item(ProdutosConhecidos.Teclado, 1) } }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_Valido_Retorna201ComLocationETagEPedidoCalculadoNoServidor()
    {
        var response = await Client.PostAsJsonAsync("/pedidos", new
        {
            clienteId = Ana,
            itens = new[] { Item(ProdutosConhecidos.Teclado, 2), Item(ProdutosConhecidos.Mouse, 1) },
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var pedido = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        var id = pedido.GetProperty("id").GetGuid();
        response.Headers.Location!.ToString().ShouldBe($"/pedidos/{id}");
        response.Headers.ETag.ShouldNotBeNull();
        response.Headers.ETag.IsWeak.ShouldBeFalse();
        pedido.GetProperty("status").GetString().ShouldBe("Created");
        pedido.GetProperty("total").GetDecimal().ShouldBe(620.00m);
        pedido.GetProperty("itens").GetArrayLength().ShouldBe(2);

        // O Location aponta para um recurso que existe de verdade.
        (await Client.GetAsync(response.Headers.Location, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_Invalido_Retorna400ValidationProblemComTodosOsErrosPorCampo()
    {
        var response = await Client.PostAsJsonAsync("/pedidos", new
        {
            clienteId = Guid.Empty,
            itens = new[] { Item(ProdutosConhecidos.Teclado, 0) },
        }, Ct);

        var problem = await LerProblemAsync(response, HttpStatusCode.BadRequest, "requisicao.validacao");
        var errors = problem.GetProperty("errors");
        errors.TryGetProperty("clienteId", out _).ShouldBeTrue();
        errors.TryGetProperty("itens[0].quantidade", out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("33333333-3333-3333-3333-333333333333", "produto.inativo")]
    [InlineData("99999999-9999-9999-9999-999999999999", "produto.inexistente")]
    public async Task Post_ProdutoInativoOuInexistente_Retorna422(string produtoId, string code)
    {
        var response = await Client.PostAsJsonAsync("/pedidos", new
        {
            clienteId = Ana,
            itens = new[] { new { produtoId = Guid.Parse(produtoId), quantidade = 1 } },
        }, Ct);

        await LerProblemAsync(response, HttpStatusCode.UnprocessableEntity, code);
    }

    [Fact]
    public async Task Get_Inexistente_Retorna404ProblemDetailsComCode()
    {
        var response = await Client.GetAsync($"/pedidos/{Guid.NewGuid()}", Ct);

        await LerProblemAsync(response, HttpStatusCode.NotFound, "pedido.nao_encontrado");
    }

    [Fact]
    public async Task Get_ExpoeSomenteOContratoPublico_SemDetalhesInternos()
    {
        var (criado, _) = await CriarPedidoAsync(Ana, Item(ProdutosConhecidos.Monitor, 1));

        var json = await Client.GetFromJsonAsync<JsonElement>($"/pedidos/{IdDe(criado)}", Ct);

        json.EnumerateObject().Select(p => p.Name).Order().ShouldBe(
            ["clienteId", "criadoEm", "enderecoEntrega", "id", "itens", "observacao", "status", "total"]);
        json.GetProperty("status").ValueKind.ShouldBe(JsonValueKind.String);
        json.GetProperty("itens")[0].EnumerateObject().Select(p => p.Name).Order().ShouldBe(
            ["nome", "precoUnitario", "produtoId", "quantidade", "subtotal"]);
        var bruto = json.GetRawText();
        bruto.ShouldNotContain("custo", Case.Insensitive);
        bruto.ShouldNotContain("antifraude", Case.Insensitive);
        bruto.ShouldNotContain("versao", Case.Insensitive);
    }

    [Fact]
    public async Task Itens_SubRecursoDoPedido_ListaOsItens_E404ParaPedidoInexistente()
    {
        var (pedido, _) = await CriarPedidoAsync(Ana, Item(ProdutosConhecidos.Teclado, 2), Item(ProdutosConhecidos.Mouse, 3));

        var response = await Client.GetAsync($"/pedidos/{IdDe(pedido)}/itens", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var itens = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        itens.ValueKind.ShouldBe(JsonValueKind.Array);
        itens.EnumerateArray().Sum(i => i.GetProperty("quantidade").GetInt32()).ShouldBe(5);
        await LerProblemAsync(await Client.GetAsync($"/pedidos/{Guid.NewGuid()}/itens", Ct), HttpStatusCode.NotFound, "pedido.nao_encontrado");
    }

    [Fact]
    public async Task Put_ItemNovo_Retorna201ComLocation_ERepetirRetorna200SemMudarOETag()
    {
        var (pedido, _) = await CriarPedidoAsync(Ana);
        var url = $"/pedidos/{IdDe(pedido)}/itens/{ProdutosConhecidos.Mouse.Id}";

        var primeira = await Client.PutAsJsonAsync(url, new { quantidade = 3 }, Ct);
        primeira.StatusCode.ShouldBe(HttpStatusCode.Created);
        primeira.Headers.Location!.ToString().ShouldBe(url);
        var etagDepoisDaPrimeira = primeira.Headers.ETag!.Tag;

        var segunda = await Client.PutAsJsonAsync(url, new { quantidade = 3 }, Ct);
        segunda.StatusCode.ShouldBe(HttpStatusCode.OK);
        segunda.Headers.ETag!.Tag.ShouldBe(etagDepoisDaPrimeira); // idempotente: mesmo estado, mesma versão
        (await segunda.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("quantidade").GetInt32().ShouldBe(3);

        var atual = await Client.GetFromJsonAsync<JsonElement>($"/pedidos/{IdDe(pedido)}", Ct);
        atual.GetProperty("total").GetDecimal().ShouldBe(250.00m + 3 * 120.00m);
    }

    [Fact]
    public async Task Put_QuantidadeInvalida_Retorna400()
    {
        var (pedido, _) = await CriarPedidoAsync(Ana);

        var response = await Client.PutAsJsonAsync($"/pedidos/{IdDe(pedido)}/itens/{ProdutosConhecidos.Mouse.Id}", new { quantidade = 0 }, Ct);

        var problem = await LerProblemAsync(response, HttpStatusCode.BadRequest, "requisicao.validacao");
        problem.GetProperty("errors").TryGetProperty("quantidade", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Delete_Item_Retorna204_ERepetirTambemRetorna204()
    {
        var (pedido, _) = await CriarPedidoAsync(Ana, Item(ProdutosConhecidos.Teclado, 1), Item(ProdutosConhecidos.Mouse, 1));
        var url = $"/pedidos/{IdDe(pedido)}/itens/{ProdutosConhecidos.Mouse.Id}";

        (await Client.DeleteAsync(url, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.DeleteAsync(url, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var itens = await Client.GetFromJsonAsync<JsonElement>($"/pedidos/{IdDe(pedido)}/itens", Ct);
        itens.GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task Confirmacao_Retorna200ComStatusConfirmed_ESegundaVezRetorna409()
    {
        var (pedido, _) = await CriarPedidoAsync(Ana);
        var url = $"/pedidos/{IdDe(pedido)}/confirmacao";

        var primeira = await Client.PostAsync(url, null, Ct);
        primeira.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await primeira.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("status").GetString().ShouldBe("Confirmed");

        await LerProblemAsync(await Client.PostAsync(url, null, Ct), HttpStatusCode.Conflict, "pedido.transicao_invalida");
    }

    [Fact]
    public async Task Put_ItemEmPedidoConfirmado_Retorna409_ECancelamentoRetorna200()
    {
        var (pedido, _) = await CriarPedidoAsync(Ana);
        var id = IdDe(pedido);
        (await Client.PostAsync($"/pedidos/{id}/confirmacao", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var put = await Client.PutAsJsonAsync($"/pedidos/{id}/itens/{ProdutosConhecidos.Mouse.Id}", new { quantidade = 1 }, Ct);
        await LerProblemAsync(put, HttpStatusCode.Conflict, "pedido.nao_editavel");

        var cancelamento = await Client.PostAsync($"/pedidos/{id}/cancelamento", null, Ct);
        cancelamento.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await cancelamento.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("status").GetString().ShouldBe("Cancelled");
    }
}
