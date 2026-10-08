namespace F1M02.Colecoes;

/// <summary>
/// Funções genéricas de relatório. Recebem <see cref="IEnumerable{T}"/> (covariante: <c>out T</c>),
/// então aceitam <c>List&lt;Produto&gt;</c> onde se pede <c>IEnumerable&lt;IComPreco&gt;</c>.
/// </summary>
public static class Relatorios
{
    /// <summary>Soma dos preços (0 para sequência vazia).</summary>
    public static decimal SomarPrecos(IEnumerable<IComPreco> itens) =>
        throw new NotImplementedException("TODO: some os preços");

    /// <summary>
    /// Item de maior preço, ou <c>null</c> se a sequência estiver vazia.
    /// Em caso de empate, o primeiro encontrado.
    /// O retorno é do MESMO tipo da entrada (por isso é genérico com constraint, e não <c>IComPreco</c>).
    /// </summary>
    public static T? MaisCaro<T>(IEnumerable<T> itens) where T : class, IComPreco =>
        throw new NotImplementedException("TODO: percorra os itens guardando o de maior preço (o constraint IComPreco libera .Preco)");

    /// <summary>
    /// Agrupa os itens por chave, preservando a ordem de chegada dentro de cada grupo.
    /// </summary>
    public static IReadOnlyDictionary<TChave, IReadOnlyList<T>> AgruparPor<T, TChave>(
        IEnumerable<T> itens, Func<T, TChave> chave) where TChave : notnull =>
        throw new NotImplementedException("TODO: use um Dictionary<TChave, List<T>> e TryGetValue");
}
