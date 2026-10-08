using F3M05.EfAvancado.Dominio;
using F3M05.EfAvancado.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace F3M05.EfAvancado.Consultas;

public sealed class PedidoConsultas(LojaDbContext db)
{
    /// <summary>
    /// Pedidos do cliente com itens E eventos (somente leitura), ordenados por Id.
    /// Duas coleções no mesmo SELECT multiplicam as linhas (itens × eventos): use split query
    /// — um comando para os pedidos, um para os itens, um para os eventos.
    /// </summary>
    public async Task<IReadOnlyList<Pedido>> ObterDoClienteComDetalhesAsync(int clienteId, CancellationToken ct = default)
    {
        return await db.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == clienteId)
            .OrderBy(p => p.Id)
            .Include(p => p.Itens)
            .Include(p => p.Eventos)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Todos os itens comprados pelo cliente, com o produto (somente leitura). Itens do mesmo
    /// produto devem apontar para a MESMA instância de <see cref="Produto"/> (identity resolution),
    /// sem encher o change tracker.
    /// </summary>
    public async Task<IReadOnlyList<ItemPedido>> ListarItensDoClienteAsync(int clienteId, CancellationToken ct = default)
    {
        return await db.ItensPedido
            .AsNoTrackingWithIdentityResolution()
            .Where(i => i.Pedido.ClienteId == clienteId)
            .Include(i => i.Produto)
            .OrderBy(i => i.Id)
            .ToListAsync(ct);
    }
}
