using F4M06.Catalogo.Contracts;
using F4M06.Catalogo.Infra;
using Microsoft.EntityFrameworkCore;

namespace F4M06.Catalogo.Fachada;

/// <summary>
/// Implementação INTERNA do contrato público <see cref="ICatalogoApi"/>.
/// Traduz a entidade privada (<c>Produto</c>) para o DTO do contrato (<see cref="ProdutoResumo"/>).
/// </summary>
internal sealed class CatalogoApi(CatalogoDbContext db) : ICatalogoApi
{
    public async Task<IReadOnlyList<ProdutoResumo>> ObterProdutosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0) return [];

        return await db.Produtos
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new ProdutoResumo(p.Id, p.Nome, p.Preco, p.Ativo))
            .ToListAsync(ct);
    }
}
