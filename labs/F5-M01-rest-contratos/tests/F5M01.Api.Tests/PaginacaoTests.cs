using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using F5M01.Api.Dominio;
using F5M01.Api.Tests.Infra;
using Microsoft.AspNetCore.WebUtilities;

namespace F5M01.Api.Tests;

/// <summary>Passo 5: coleção com filtro, ordenação e paginação (metadados + links).</summary>
public sealed class PaginacaoTests : ApiTestBase
{
    private static Dictionary<string, string> Query(string? link)
    {
        link.ShouldNotBeNull();
        var indice = link.IndexOf('?', StringComparison.Ordinal);
        link[..indice].ShouldBe("/pedidos");
        return QueryHelpers.ParseQuery(link[indice..]).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
    }

    [Fact]
    public async Task Listar_PaginaDoMeio_TemMetadadosELinksDeNavegacao()
    {
        for (var i = 0; i < 5; i++) await CriarPedidoAsync(Ana);

        var json = await Client.GetFromJsonAsync<JsonElement>("/pedidos?pagina=2&tamanhoPagina=2", Ct);

        json.GetProperty("itens").GetArrayLength().ShouldBe(2);
        json.GetProperty("pagina").GetInt32().ShouldBe(2);
        json.GetProperty("tamanhoPagina").GetInt32().ShouldBe(2);
        json.GetProperty("totalItens").GetInt32().ShouldBe(5);
        json.GetProperty("totalPaginas").GetInt32().ShouldBe(3);

        var links = json.GetProperty("links");
        Query(links.GetProperty("self").GetString())["pagina"].ShouldBe("2");
        Query(links.GetProperty("first").GetString())["pagina"].ShouldBe("1");
        Query(links.GetProperty("prev").GetString())["pagina"].ShouldBe("1");
        Query(links.GetProperty("next").GetString())["pagina"].ShouldBe("3");
        var last = Query(links.GetProperty("last").GetString());
        last["pagina"].ShouldBe("3");
        last["tamanhoPagina"].ShouldBe("2");

        // Item de lista é resumido: sem a coleção de itens.
        json.GetProperty("itens")[0].TryGetProperty("itens", out _).ShouldBeFalse();
        json.GetProperty("itens")[0].GetProperty("quantidadeItens").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Listar_PrimeiraEUltimaPagina_NaoTemPrevNemNext_EPadraoEOrdemDeCriacao()
    {
        var criados = new List<Guid>();
        for (var i = 0; i < 3; i++) criados.Add(IdDe((await CriarPedidoAsync(Ana)).Pedido));

        var json = await Client.GetFromJsonAsync<JsonElement>("/pedidos", Ct);

        json.GetProperty("tamanhoPagina").GetInt32().ShouldBe(20);
        json.GetProperty("totalPaginas").GetInt32().ShouldBe(1);
        json.GetProperty("links").GetProperty("prev").ValueKind.ShouldBe(JsonValueKind.Null);
        json.GetProperty("links").GetProperty("next").ValueKind.ShouldBe(JsonValueKind.Null);
        json.GetProperty("itens").EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ShouldBe(criados);
    }

    [Fact]
    public async Task Listar_FiltraPorStatusECliente_OrdenaPorTotalDecrescente_ELinksPreservamOsFiltros()
    {
        var (barato, _) = await CriarPedidoAsync(Ana, Item(ProdutosConhecidos.Mouse, 1));            // 120
        var (caro, _) = await CriarPedidoAsync(Ana, Item(ProdutosConhecidos.Monitor, 1));            // 1500
        var (medio, _) = await CriarPedidoAsync(Ana, Item(ProdutosConhecidos.Teclado, 2));           // 500
        var (deOutro, _) = await CriarPedidoAsync(Bruno, Item(ProdutosConhecidos.Monitor, 2));       // 3000, outro cliente
        await CriarPedidoAsync(Ana, Item(ProdutosConhecidos.Monitor, 3));                            // 4500, fica Created
        foreach (var p in new[] { barato, caro, medio, deOutro })
            (await Client.PostAsync($"/pedidos/{IdDe(p)}/confirmacao", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await Client.GetFromJsonAsync<JsonElement>(
            $"/pedidos?status=confirmed&clienteId={Ana}&ordenarPor=-total&tamanhoPagina=2", Ct);

        json.GetProperty("totalItens").GetInt32().ShouldBe(3);
        json.GetProperty("itens").EnumerateArray().Select(p => p.GetProperty("total").GetDecimal()).ShouldBe([1500.00m, 500.00m]);
        json.GetProperty("itens").EnumerateArray().ShouldAllBe(p => p.GetProperty("status").GetString() == "Confirmed");

        var next = Query(json.GetProperty("links").GetProperty("next").GetString());
        next["pagina"].ShouldBe("2");
        next["status"].ShouldBe("confirmed");
        next["clienteId"].ShouldBe(Ana.ToString());
        next["ordenarPor"].ShouldBe("-total");
    }

    [Theory]
    [InlineData("tamanhoPagina=0", "tamanhoPagina")]
    [InlineData("tamanhoPagina=101", "tamanhoPagina")]
    [InlineData("pagina=0", "pagina")]
    [InlineData("ordenarPor=custoInterno", "ordenarPor")]
    [InlineData("status=Enviado", "status")]
    public async Task Listar_ParametroInvalido_Retorna400ValidationProblem(string query, string campo)
    {
        var response = await Client.GetAsync($"/pedidos?{query}", Ct);

        var problem = await LerProblemAsync(response, HttpStatusCode.BadRequest, "requisicao.validacao");
        problem.GetProperty("errors").TryGetProperty(campo, out _).ShouldBeTrue();
    }
}
