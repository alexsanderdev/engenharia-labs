using F2M07.Api.Infrastructure;
using F2M07.Api.Pedidos;
using F2M07.Api.Produtos;

namespace F2M07.Api.Tests.Infra;

/// <summary>
/// Base dos testes de integração: limpa o banco ANTES de cada teste e oferece helpers
/// de Arrange (semear dados direto no banco) e de Assert (consultar o efeito persistido).
/// Já vem pronta: o trabalho do lab está em ApiFixture, PedidosApiFactory e TestAuthHandler.
/// </summary>
public abstract class IntegracaoTestBase(ApiFixture fixture) : IAsyncLifetime
{
    protected ApiFixture Fixture { get; } = fixture;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Cliente "logado" usado na maioria dos testes.</summary>
    protected static readonly Guid ClienteA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    /// <summary>Outro cliente, para testes de isolamento entre usuários.</summary>
    protected static readonly Guid ClienteB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public virtual async ValueTask InitializeAsync() => await Fixture.ResetarBancoAsync();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>HttpClient da API; com <paramref name="clienteId"/>, autenticado como esse cliente.</summary>
    protected HttpClient CriarCliente(Guid? clienteId = null)
    {
        var client = Fixture.Factory.CreateClient();
        if (clienteId is { } id)
            client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderClienteId, id.ToString());
        return client;
    }

    protected Task ComBancoAsync(Func<PedidosDbContext, Task> acao) => Fixture.ComBancoAsync(acao);

    protected Task<T> ComBancoAsync<T>(Func<PedidosDbContext, Task<T>> consulta) => Fixture.ComBancoAsync(consulta);

    /// <summary>Arrange direto no banco: mais rápido e mais focado do que chamar POST /produtos.</summary>
    protected async Task<Produto> SemearProdutoAsync(string sku, decimal preco, bool ativo = true)
    {
        var produto = new Produto(Guid.NewGuid(), sku, $"Produto {sku}", preco, ativo);
        await ComBancoAsync(async db =>
        {
            db.Produtos.Add(produto);
            await db.SaveChangesAsync(Ct);
        });
        return produto;
    }

    /// <summary>Semeia um pedido do cliente e o leva até o status pedido (Created, Confirmed ou Completed).</summary>
    protected async Task<Pedido> SemearPedidoAsync(Guid clienteId, Produto produto, StatusPedido status)
    {
        var pedido = new Pedido(Guid.NewGuid(), clienteId, Fixture.Relogio.GetUtcNow());
        pedido.AdicionarItem(produto, 1);
        if (status is StatusPedido.Confirmed or StatusPedido.Completed) pedido.Confirmar();
        if (status is StatusPedido.Completed) pedido.Concluir();

        await ComBancoAsync(async db =>
        {
            db.Pedidos.Add(pedido);
            await db.SaveChangesAsync(Ct);
        });
        return pedido;
    }
}
