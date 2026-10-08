using System.Net;
using System.Net.Http.Json;

namespace F4M06.Integracao.Tests.Infra;

// DTOs do lado do TESTE: o teste conversa com os módulos como um cliente HTTP qualquer.
// (As entidades e os DTOs dos módulos são internal — e é assim que deve ser.)
public sealed record ProdutoDto(Guid Id, string Nome, decimal Preco, bool Ativo);

public sealed record ClienteDto(Guid Id, string Nome, string Email, decimal TotalGasto, int PedidosConfirmados, string Nivel);

public sealed record ItemPedidoDto(Guid ProdutoId, string NomeProduto, decimal PrecoUnitario, int Quantidade);

public sealed record PedidoDto(Guid Id, Guid ClienteId, string Status, decimal Total, List<ItemPedidoDto> Itens);

/// <summary>
/// JÁ VEM PRONTA. Base dos testes de integração: limpa o banco ANTES de cada teste e oferece helpers
/// que usam a API HTTP pública de cada módulo (nada de semear direto em tabela de módulo).
/// </summary>
public abstract class IntegracaoTestBase(OrderFlowFixture fixture) : IAsyncLifetime
{
    protected OrderFlowFixture Fixture { get; } = fixture;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected HttpClient Api { get; } = fixture.Factory.CreateClient();

    public virtual async ValueTask InitializeAsync() => await Fixture.ResetarBancoAsync();

    public ValueTask DisposeAsync()
    {
        Api.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected async Task<ProdutoDto> CriarProdutoAsync(string nome, decimal preco)
    {
        var resposta = await Api.PostAsJsonAsync("/catalogo/produtos", new { nome, preco }, Ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync(Ct));
        return (await resposta.Content.ReadFromJsonAsync<ProdutoDto>(Ct))!;
    }

    protected async Task DesativarProdutoAsync(Guid produtoId)
    {
        var resposta = await Api.PostAsync($"/catalogo/produtos/{produtoId}/desativar", null, Ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    protected async Task AlterarPrecoAsync(Guid produtoId, decimal preco)
    {
        var resposta = await Api.PutAsJsonAsync($"/catalogo/produtos/{produtoId}/preco", new { preco }, Ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    protected async Task<ClienteDto> CriarClienteAsync(string nome = "Ana")
    {
        var resposta = await Api.PostAsJsonAsync("/clientes", new { nome, email = $"{nome.ToLowerInvariant()}@exemplo.com" }, Ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync(Ct));
        return (await resposta.Content.ReadFromJsonAsync<ClienteDto>(Ct))!;
    }

    protected async Task<ClienteDto> ObterClienteAsync(Guid clienteId) =>
        (await Api.GetFromJsonAsync<ClienteDto>($"/clientes/{clienteId}", Ct))!;

    /// <summary>POST /pedidos "cru": devolve a resposta para o teste conferir status e corpo.</summary>
    protected Task<HttpResponseMessage> PostarPedidoAsync(Guid clienteId, params (Guid ProdutoId, int Quantidade)[] itens) =>
        Api.PostAsJsonAsync(
            "/pedidos",
            new { clienteId, itens = itens.Select(i => new { produtoId = i.ProdutoId, quantidade = i.Quantidade }) },
            Ct);

    protected async Task<PedidoDto> CriarPedidoAsync(Guid clienteId, params (Guid ProdutoId, int Quantidade)[] itens)
    {
        var resposta = await PostarPedidoAsync(clienteId, itens);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync(Ct));
        return (await resposta.Content.ReadFromJsonAsync<PedidoDto>(Ct))!;
    }

    protected Task<HttpResponseMessage> ConfirmarPedidoAsync(Guid pedidoId) =>
        Api.PostAsync($"/pedidos/{pedidoId}/confirmar", null, Ct);
}
