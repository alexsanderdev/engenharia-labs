using System.Collections.Concurrent;
using F2M08.Domain.Entidades;
using F2M08.Domain.Repositorios;

namespace F2M08.Infrastructure.Persistencia;

public sealed class PedidoRepositorioEmMemoria : IPedidoRepository
{
    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();

    public Task<Pedido?> ObterAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_pedidos.GetValueOrDefault(id));

    public Task AdicionarAsync(Pedido pedido, CancellationToken ct)
    {
        _pedidos[pedido.Id] = pedido;
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken ct) => Task.CompletedTask;
}

public sealed class ProdutoRepositorioEmMemoria : IProdutoRepository
{
    private readonly ConcurrentDictionary<Guid, Produto> _produtos = new();

    public void Adicionar(Produto produto) => _produtos[produto.Id] = produto;

    public Task<IReadOnlyList<Produto>> ObterVariosAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        IReadOnlyList<Produto> encontrados = [.. ids.Distinct().Select(_produtos.GetValueOrDefault).OfType<Produto>()];
        return Task.FromResult(encontrados);
    }
}
