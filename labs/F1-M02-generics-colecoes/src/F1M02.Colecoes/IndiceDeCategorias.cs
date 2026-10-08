namespace F1M02.Colecoes;

/// <summary>
/// Índice categoria → produtos. Categorias ordenadas alfabeticamente e sem diferenciar maiúsculas;
/// cada categoria guarda ids SEM duplicatas.
/// Estruturas sugeridas: <see cref="SortedDictionary{TKey, TValue}"/> + <see cref="HashSet{T}"/>.
/// </summary>
public sealed class IndiceDeCategorias
{
    // TODO: declare um SortedDictionary<string, HashSet<Guid>> com StringComparer.OrdinalIgnoreCase.

    /// <summary>
    /// Associa o produto à categoria. Retorna <c>false</c> se a associação já existia.
    /// Categoria em branco lança <see cref="ArgumentException"/>.
    /// </summary>
    public bool Adicionar(string categoria, Guid produtoId) =>
        throw new NotImplementedException("TODO: crie o HashSet da categoria se não existir e use o retorno de HashSet.Add");

    /// <summary>Ids da categoria (vazio se a categoria não existir). Snapshot somente leitura.</summary>
    public IReadOnlyCollection<Guid> ProdutosDa(string categoria) =>
        throw new NotImplementedException("TODO: retorne uma cópia dos ids ou uma coleção vazia");

    /// <summary>Categorias em ordem alfabética (com a grafia do primeiro cadastro).</summary>
    public IReadOnlyList<string> Categorias() =>
        throw new NotImplementedException("TODO: retorne as chaves (o SortedDictionary já mantém a ordem)");
}
