using F4M01.Application.Abstracoes;
using F4M01.Domain.Pedidos;
using F4M01.Domain.Produtos;
using Microsoft.EntityFrameworkCore;

namespace F4M01.Infrastructure.Persistencia;

/// <summary>ADAPTADOR da porta <see cref="IPedidoRepository"/> usando EF Core.</summary>
internal sealed class PedidoRepositoryEf(OrderFlowDbContext db) : IPedidoRepository
{
    public async Task AdicionarAsync(Pedido pedido, CancellationToken ct)
    {
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Pedido>> ListarPorClienteAsync(Guid clienteId, CancellationToken ct) =>
        await db.Pedidos.AsNoTracking().Where(p => p.ClienteId == clienteId).ToListAsync(ct);
}

/// <summary>ADAPTADOR da porta <see cref="IProdutoRepository"/> usando EF Core.</summary>
internal sealed class ProdutoRepositoryEf(OrderFlowDbContext db) : IProdutoRepository
{
    public async Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        await db.Produtos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync(ct);
}
