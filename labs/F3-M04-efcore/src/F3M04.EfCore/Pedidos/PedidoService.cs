using F3M04.EfCore.Dominio;
using F3M04.EfCore.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace F3M04.EfCore.Pedidos;

/// <summary>
/// Serviço de aplicação de pedidos: um caso de uso por método, um <c>SaveChanges</c> por
/// caso de uso. Leituras para tela usam projeção (DTO) ou <c>AsNoTracking</c>; escritas
/// carregam a entidade rastreada e deixam o change tracker gerar o UPDATE.
/// </summary>
public sealed class PedidoService(OrderFlowDbContext db, TimeProvider relogio)
{
    /// <summary>
    /// Produtos ativos ordenados por nome, SEM tracking (somente leitura): o change tracker
    /// do contexto deve continuar vazio depois da chamada.
    /// </summary>
    public async Task<IReadOnlyList<Produto>> ListarProdutosAtivosAsync(CancellationToken ct = default)
    {
        return await db.Produtos
            .AsNoTracking()
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Cria um pedido para o cliente. Carrega TODOS os produtos dos itens numa única consulta;
    /// se algum não existir, lança <see cref="InvalidOperationException"/>; produto inativo é
    /// barrado pelo domínio (<see cref="Pedido.AdicionarItem"/>). Total calculado com o preço do
    /// banco; data vinda do <see cref="TimeProvider"/>. Devolve o id do pedido criado.
    /// </summary>
    public async Task<Guid> CriarAsync(Guid clienteId, IReadOnlyList<ItemSolicitado> itens, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(itens);
        if (itens.Count == 0)
            throw new ArgumentException("O pedido precisa de ao menos um item.", nameof(itens));

        var ids = itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await db.Produtos
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var faltando = ids.Where(id => !produtos.ContainsKey(id)).ToList();
        if (faltando.Count > 0)
            throw new InvalidOperationException($"Produto(s) inexistente(s): {string.Join(", ", faltando)}.");

        var pedido = new Pedido(clienteId, relogio.GetUtcNow());
        foreach (var item in itens)
            pedido.AdicionarItem(produtos[item.ProdutoId], item.Quantidade);

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);
        return pedido.Id;
    }

    /// <summary>
    /// Carrega o pedido COM os itens (eager loading com <c>Include</c>) e RASTREADO, pronto para
    /// ser alterado e salvo. Devolve <c>null</c> se não existir.
    /// </summary>
    public Task<Pedido?> ObterComItensAsync(Guid id, CancellationToken ct = default)
    {
        return db.Pedidos
            .Include(p => p.Itens)
            .SingleOrDefaultAsync(p => p.Id == id, ct);
    }

    /// <summary>
    /// Projeção direta para <see cref="PedidoResumoDto"/> com <c>Select</c>: nome do cliente e
    /// itens com nome do produto (ordenados por nome). Nenhuma entidade é materializada nem
    /// rastreada. Devolve <c>null</c> se o pedido não existir.
    /// </summary>
    public async Task<PedidoResumoDto?> ObterResumoAsync(Guid id, CancellationToken ct = default)
    {
        var consulta =
            from p in db.Pedidos
            join c in db.Clientes on p.ClienteId equals c.Id
            where p.Id == id
            select new PedidoResumoDto(
                p.Id,
                c.Nome,
                p.Status,
                p.Total,
                p.CriadoEm,
                (from i in p.Itens
                 join pr in db.Produtos on i.ProdutoId equals pr.Id
                 orderby pr.Nome
                 select new ItemResumoDto(pr.Nome, i.Quantidade, i.PrecoUnitario, i.Quantidade * i.PrecoUnitario))
                .ToList());

        return await consulta.SingleOrDefaultAsync(ct);
    }

    /// <summary>
    /// Pedidos do cliente, do mais recente para o mais antigo, projetados para
    /// <see cref="PedidoListaDto"/> (quantidade de itens calculada no SQL, sem carregar os itens).
    /// </summary>
    public async Task<IReadOnlyList<PedidoListaDto>> ListarDoClienteAsync(Guid clienteId, CancellationToken ct = default)
    {
        return await db.Pedidos
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new PedidoListaDto(p.Id, p.CriadoEm, p.Status, p.Itens.Count, p.Total))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Confirma o pedido: carrega rastreado (com os itens, que a regra de domínio exige),
    /// chama <see cref="Pedido.Confirmar"/> e salva. Pedido inexistente: <see cref="KeyNotFoundException"/>.
    /// </summary>
    public async Task ConfirmarAsync(Guid id, CancellationToken ct = default)
    {
        var pedido = await ObterComItensAsync(id, ct)
            ?? throw new KeyNotFoundException($"Pedido {id} não encontrado.");

        pedido.Confirmar();
        await db.SaveChangesAsync(ct);
    }
}
