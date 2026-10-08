using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using F5M01.Api.Dominio;
using F5M01.Api.Tests.Infra;

namespace F5M01.Api.Tests;

/// <summary>Passo 2 (na API): GET condicional (304) e concorrência otimista com If-Match (412).</summary>
public sealed class CacheEConcorrenciaTests : ApiTestBase
{
    [Fact]
    public async Task Get_RetornaETagECacheControl_EIfNoneMatchIgualRetorna304SemCorpo()
    {
        var (pedido, _) = await CriarPedidoAsync(Ana);
        var url = $"/pedidos/{IdDe(pedido)}";

        var primeira = await Client.GetAsync(url, Ct);
        primeira.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = primeira.Headers.ETag!;
        primeira.Headers.CacheControl!.NoCache.ShouldBeTrue();
        primeira.Headers.CacheControl.Private.ShouldBeTrue();

        using var condicional = new HttpRequestMessage(HttpMethod.Get, url);
        condicional.Headers.IfNoneMatch.Add(etag);
        var segunda = await Client.SendAsync(condicional, Ct);

        segunda.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        segunda.Headers.ETag!.Tag.ShouldBe(etag.Tag);
        (await segunda.Content.ReadAsByteArrayAsync(Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_IfNoneMatchDesatualizado_Retorna200ComONovoETag()
    {
        var (pedido, etagAntigo) = await CriarPedidoAsync(Ana);
        var id = IdDe(pedido);
        (await Client.PutAsJsonAsync($"/pedidos/{id}/itens/{ProdutosConhecidos.Mouse.Id}", new { quantidade = 1 }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        using var condicional = new HttpRequestMessage(HttpMethod.Get, $"/pedidos/{id}");
        condicional.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etagAntigo));
        var response = await Client.SendAsync(condicional, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag!.Tag.ShouldNotBe(etagAntigo);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("itens").GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task Put_IfMatchDesatualizado_Retorna412_ENaoAlteraOPedido()
    {
        // Duas atendentes leem o mesmo pedido; a primeira salva; a segunda tenta salvar com a versão velha.
        var (pedido, etagLido) = await CriarPedidoAsync(Ana);
        var id = IdDe(pedido);
        var urlMouse = $"/pedidos/{id}/itens/{ProdutosConhecidos.Mouse.Id}";

        var primeira = await Client.SendAsync(Requisicao(HttpMethod.Put, urlMouse, new { quantidade = 2 }, etagLido), Ct);
        primeira.StatusCode.ShouldBe(HttpStatusCode.Created);

        var segunda = await Client.SendAsync(Requisicao(HttpMethod.Put, urlMouse, new { quantidade = 5 }, etagLido), Ct);
        await LerProblemAsync(segunda, HttpStatusCode.PreconditionFailed, "pedido.versao_desatualizada");

        var atual = await Client.GetFromJsonAsync<JsonElement>($"/pedidos/{id}", Ct);
        atual.GetProperty("itens").EnumerateArray()
            .Single(i => i.GetProperty("produtoId").GetGuid() == ProdutosConhecidos.Mouse.Id)
            .GetProperty("quantidade").GetInt32().ShouldBe(2); // a primeira escrita não foi perdida (lost update evitado)
    }

    [Fact]
    public async Task Put_IfMatchAtual_AplicaEDevolveNovoETag_EDeleteComETagVelhoRetorna412()
    {
        var (pedido, etag) = await CriarPedidoAsync(Ana, Item(ProdutosConhecidos.Teclado, 1), Item(ProdutosConhecidos.Mouse, 1));
        var url = $"/pedidos/{IdDe(pedido)}/itens/{ProdutosConhecidos.Teclado.Id}";

        var put = await Client.SendAsync(Requisicao(HttpMethod.Put, url, new { quantidade = 4 }, etag), Ct);
        put.StatusCode.ShouldBe(HttpStatusCode.OK);
        var novoETag = put.Headers.ETag!.Tag;
        novoETag.ShouldNotBe(etag);

        var delete = await Client.SendAsync(Requisicao(HttpMethod.Delete, url, ifMatch: etag), Ct);
        await LerProblemAsync(delete, HttpStatusCode.PreconditionFailed, "pedido.versao_desatualizada");

        (await Client.SendAsync(Requisicao(HttpMethod.Delete, url, ifMatch: novoETag), Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Confirmacao_ComIfMatchDesatualizado_Retorna412()
    {
        var (pedido, etagVelho) = await CriarPedidoAsync(Ana);
        var id = IdDe(pedido);
        (await Client.PutAsJsonAsync($"/pedidos/{id}/itens/{ProdutosConhecidos.Mouse.Id}", new { quantidade = 1 }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await Client.SendAsync(Requisicao(HttpMethod.Post, $"/pedidos/{id}/confirmacao", ifMatch: etagVelho), Ct);

        await LerProblemAsync(response, HttpStatusCode.PreconditionFailed, "pedido.versao_desatualizada");
    }
}
