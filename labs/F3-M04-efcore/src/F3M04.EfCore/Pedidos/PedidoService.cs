using F3M04.EfCore.Dominio;
using F3M04.EfCore.Persistencia;

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
    public Task<IReadOnlyList<Produto>> ListarProdutosAtivosAsync(CancellationToken ct = default)
    {
        _ = db;
        throw new NotImplementedException("TODO (Passo 6): db.Produtos.AsNoTracking().Where(ativo).OrderBy(nome).ToListAsync(ct).");
    }

    /// <summary>
    /// Cria um pedido para o cliente. Carrega TODOS os produtos dos itens numa única consulta;
    /// se algum não existir, lança <see cref="InvalidOperationException"/>; produto inativo é
    /// barrado pelo domínio (<see cref="Pedido.AdicionarItem"/>). Total calculado com o preço do
    /// banco; data vinda do <see cref="TimeProvider"/>. Devolve o id do pedido criado.
    /// </summary>
    public Task<Guid> CriarAsync(Guid clienteId, IReadOnlyList<ItemSolicitado> itens, CancellationToken ct = default)
    {
        _ = relogio;
        throw new NotImplementedException("TODO (Passo 7): carregue os produtos com ids.Contains(p.Id) numa consulta, crie o Pedido com relogio.GetUtcNow(), AdicionarItem, Add e SaveChangesAsync.");
    }

    /// <summary>
    /// Carrega o pedido COM os itens (eager loading com <c>Include</c>) e RASTREADO, pronto para
    /// ser alterado e salvo. Devolve <c>null</c> se não existir.
    /// </summary>
    public Task<Pedido?> ObterComItensAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("TODO (Passo 6): db.Pedidos.Include(p => p.Itens).SingleOrDefaultAsync(...) — com tracking.");
    }

    /// <summary>
    /// Projeção direta para <see cref="PedidoResumoDto"/> com <c>Select</c>: nome do cliente e
    /// itens com nome do produto (ordenados por nome). Nenhuma entidade é materializada nem
    /// rastreada. Devolve <c>null</c> se o pedido não existir.
    /// </summary>
    public Task<PedidoResumoDto?> ObterResumoAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("TODO (Passo 6): projete com Select/join para PedidoResumoDto (Cliente e Produto entram por join, não por Include).");
    }

    /// <summary>
    /// Pedidos do cliente, do mais recente para o mais antigo, projetados para
    /// <see cref="PedidoListaDto"/> (quantidade de itens calculada no SQL, sem carregar os itens).
    /// </summary>
    public Task<IReadOnlyList<PedidoListaDto>> ListarDoClienteAsync(Guid clienteId, CancellationToken ct = default)
    {
        throw new NotImplementedException("TODO (Passo 6): Where(cliente).OrderByDescending(CriadoEm).Select(p => new PedidoListaDto(..., p.Itens.Count, ...)).");
    }

    /// <summary>
    /// Confirma o pedido: carrega rastreado (com os itens, que a regra de domínio exige),
    /// chama <see cref="Pedido.Confirmar"/> e salva. Pedido inexistente: <see cref="KeyNotFoundException"/>.
    /// </summary>
    public Task ConfirmarAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("TODO (Passo 8): ObterComItensAsync, pedido.Confirmar(), SaveChangesAsync — o change tracker gera o UPDATE.");
    }
}
