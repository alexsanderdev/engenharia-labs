namespace F1M02.Colecoes;

/// <summary>
/// Funções genéricas de relatório. Recebem <see cref="IEnumerable{T}"/> (covariante: <c>out T</c>),
/// então aceitam <c>List&lt;Produto&gt;</c> onde se pede <c>IEnumerable&lt;IComPreco&gt;</c>.
/// </summary>
public static class Relatorios
{
    /// <summary>Soma dos preços (0 para sequência vazia).</summary>
    public static decimal SomarPrecos(IEnumerable<IComPreco> itens)
    {
        ArgumentNullException.ThrowIfNull(itens);
        return itens.Sum(i => i.Preco);
    }

    /// <summary>
    /// Item de maior preço, ou <c>null</c> se a sequência estiver vazia.
    /// Em caso de empate, o primeiro encontrado.
    /// O retorno é do MESMO tipo da entrada (por isso é genérico com constraint, e não <c>IComPreco</c>).
    /// </summary>
    public static T? MaisCaro<T>(IEnumerable<T> itens) where T : class, IComPreco
    {
        ArgumentNullException.ThrowIfNull(itens);
        T? maisCaro = null;
        foreach (var item in itens)
        {
            if (maisCaro is null || item.Preco > maisCaro.Preco)
                maisCaro = item;
        }
        return maisCaro;
    }

    /// <summary>
    /// Agrupa os itens por chave, preservando a ordem de chegada dentro de cada grupo.
    /// </summary>
    public static IReadOnlyDictionary<TChave, IReadOnlyList<T>> AgruparPor<T, TChave>(
        IEnumerable<T> itens, Func<T, TChave> chave) where TChave : notnull
    {
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(chave);

        var grupos = new Dictionary<TChave, List<T>>();
        foreach (var item in itens)
        {
            var k = chave(item);
            if (!grupos.TryGetValue(k, out var lista))
            {
                lista = [];
                grupos[k] = lista;
            }
            lista.Add(item);
        }
        return grupos.ToDictionary(g => g.Key, g => (IReadOnlyList<T>)g.Value);
    }
}
