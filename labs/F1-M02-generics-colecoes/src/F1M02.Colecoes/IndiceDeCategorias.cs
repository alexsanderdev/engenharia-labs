namespace F1M02.Colecoes;

/// <summary>
/// Índice categoria → produtos. Categorias ordenadas alfabeticamente e sem diferenciar maiúsculas;
/// cada categoria guarda ids SEM duplicatas.
/// Estruturas sugeridas: <see cref="SortedDictionary{TKey, TValue}"/> + <see cref="HashSet{T}"/>.
/// </summary>
public sealed class IndiceDeCategorias
{
    private readonly SortedDictionary<string, HashSet<Guid>> _indice = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Associa o produto à categoria. Retorna <c>false</c> se a associação já existia.
    /// Categoria em branco lança <see cref="ArgumentException"/>.
    /// </summary>
    public bool Adicionar(string categoria, Guid produtoId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoria);
        if (!_indice.TryGetValue(categoria, out var ids))
        {
            ids = [];
            _indice[categoria] = ids;
        }
        return ids.Add(produtoId);
    }

    /// <summary>Ids da categoria (vazio se a categoria não existir). Snapshot somente leitura.</summary>
    public IReadOnlyCollection<Guid> ProdutosDa(string categoria) =>
        _indice.TryGetValue(categoria, out var ids) ? [.. ids] : [];

    /// <summary>Categorias em ordem alfabética (com a grafia do primeiro cadastro).</summary>
    public IReadOnlyList<string> Categorias() => [.. _indice.Keys];
}
