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
        _itens.Add(item);
        var i = _itens.Count - 1;
        while (i > 0)
        {
            var pai = (i - 1) / 2;
            if (_comparador.Compare(_itens[i], _itens[pai]) >= 0)
                break;

            (_itens[i], _itens[pai]) = (_itens[pai], _itens[i]);
            i = pai;
        }
    }

    /// <summary>
    /// Remove e devolve o menor item: troca a raiz pelo último, remove o último e
    /// "desce" (sift-down) a nova raiz trocando com o menor filho. Heap vazio: <see cref="InvalidOperationException"/>.
    /// </summary>
    public T Remover()
    {
        var menor = Espiar();
        var ultimo = _itens.Count - 1;
        _itens[0] = _itens[ultimo];
        _itens.RemoveAt(ultimo);

        var i = 0;
        while (true)
        {
            var esquerdo = 2 * i + 1;
            var direito = esquerdo + 1;
            var alvo = i;

            if (esquerdo < _itens.Count && _comparador.Compare(_itens[esquerdo], _itens[alvo]) < 0)
                alvo = esquerdo;
            if (direito < _itens.Count && _comparador.Compare(_itens[direito], _itens[alvo]) < 0)
                alvo = direito;
            if (alvo == i)
                break;

            (_itens[i], _itens[alvo]) = (_itens[alvo], _itens[i]);
            i = alvo;
        }

        return menor;
    }

    /// <summary>Devolve o menor item sem remover. Heap vazio: <see cref="InvalidOperationException"/>.</summary>
    public T Espiar()
    {
        if (_itens.Count == 0)
            throw new InvalidOperationException("O heap está vazio.");

        return _itens[0];
    }
}
