namespace PM01.Estruturas;

/// <summary>
/// Heap binário mínimo guardado em array, base de uma fila de prioridade
/// (equivalente didático de <see cref="PriorityQueue{TElement, TPriority}"/>).
/// Para o nó no índice i: pai = (i - 1) / 2, filho esquerdo = 2i + 1, filho direito = 2i + 2.
/// Inserir e remover são O(log n); espiar o menor é O(1).
/// </summary>
public sealed class MinHeap<T>
{
    private readonly IComparer<T> _comparador;
    private readonly List<T> _itens = [];

    /// <summary>Cria o heap. Sem comparador, usa <see cref="Comparer{T}.Default"/>.</summary>
    public MinHeap(IComparer<T>? comparador = null)
    {
        _comparador = comparador ?? Comparer<T>.Default;
    }

    /// <summary>Quantidade de itens no heap.</summary>
    public int Count => _itens.Count;

    /// <summary>Coloca o item no fim do array e o "sobe" (sift-up) enquanto for menor que o pai.</summary>
    public void Inserir(T item)
    {
        throw new NotImplementedException("TODO: _itens.Add(item) e troque com o pai ((i - 1) / 2) enquanto for menor.");
    }

    /// <summary>
    /// Remove e devolve o menor item: troca a raiz pelo último, remove o último e
    /// "desce" (sift-down) a nova raiz trocando com o menor filho. Heap vazio: <see cref="InvalidOperationException"/>.
    /// </summary>
    public T Remover()
    {
        throw new NotImplementedException("TODO: guarde _itens[0], mova o último para a raiz, remova o último e faça o sift-down com o MENOR filho.");
    }

    /// <summary>Devolve o menor item sem remover. Heap vazio: <see cref="InvalidOperationException"/>.</summary>
    public T Espiar()
    {
        throw new NotImplementedException("TODO: devolva _itens[0] ou lance InvalidOperationException se vazio.");
    }
}
