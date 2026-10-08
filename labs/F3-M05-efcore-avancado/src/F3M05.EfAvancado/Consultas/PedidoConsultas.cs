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
        // CONSULTA RUIM (explosão cartesiana): UM comando, mas com JOIN nas duas coleções.
        // Um pedido com 50 itens e 40 eventos devolve 2.000 linhas em vez de 91.
        // TODO (Passo 5): resolva com AsSplitQuery().
        return await db.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == clienteId)
            .OrderBy(p => p.Id)
            .Include(p => p.Itens)
            .Include(p => p.Eventos)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Todos os itens comprados pelo cliente, com o produto (somente leitura). Itens do mesmo
    /// produto devem apontar para a MESMA instância de <see cref="Produto"/> (identity resolution),
    /// sem encher o change tracker.
    /// </summary>
    public async Task<IReadOnlyList<ItemPedido>> ListarItensDoClienteAsync(int clienteId, CancellationToken ct = default)
    {
        // CONSULTA RUIM (sutil): AsNoTracking puro cria uma instância de Produto POR LINHA.
        // 10.000 itens do mesmo produto = 10.000 objetos Produto iguais na memória.
        // TODO (Passo 6): troque por AsNoTrackingWithIdentityResolution().
        return await db.ItensPedido
            .AsNoTracking()
            .Where(i => i.Pedido.ClienteId == clienteId)
            .Include(i => i.Produto)
            .OrderBy(i => i.Id)
            .ToListAsync(ct);
    }
}
