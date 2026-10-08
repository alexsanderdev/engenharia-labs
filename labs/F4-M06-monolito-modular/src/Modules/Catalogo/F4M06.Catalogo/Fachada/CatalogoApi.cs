using F4M06.Catalogo.Contracts;

namespace F4M06.Catalogo.Fachada;

/// <summary>
/// Implementação do contrato público <see cref="ICatalogoApi"/>.
/// Deve traduzir a entidade privada (<c>Produto</c>) para o DTO do contrato (<see cref="ProdutoResumo"/>).
/// </summary>
public sealed class CatalogoApi : ICatalogoApi
{
    public Task<IReadOnlyList<ProdutoResumo>> ObterProdutosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 2): receba o CatalogoDbContext pelo construtor e devolva, numa ÚNICA consulta " +
            "(AsNoTracking + Where(ids.Contains) + Select), um ProdutoResumo por produto encontrado. " +
            "Depois registre ICatalogoApi -> CatalogoApi no CatalogoModule.Register e torne esta classe internal.");
}
