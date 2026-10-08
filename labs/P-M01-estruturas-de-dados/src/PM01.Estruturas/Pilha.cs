namespace PM01.Estruturas;

/// <summary>
/// Pilha LIFO (último a entrar, primeiro a sair), equivalente simplificado de <see cref="Stack{T}"/>.
/// Todas as operações são O(1) (empilhar é O(1) amortizado).
/// </summary>
public sealed class Pilha<T>
{
    private T[] _itens = new T[4];

    /// <summary>Quantidade de itens na pilha.</summary>
    public int Count { get; private set; }

    /// <summary>Coloca o item no topo. Dobra o array interno quando enche.</summary>
    public void Empilhar(T item)
    {
        if (Count == _itens.Length)
            Array.Resize(ref _itens, _itens.Length * 2);

        _itens[Count++] = item;
    }

    /// <summary>Remove e devolve o item do topo. Pilha vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Desempilhar()
    {
        if (!TentarDesempilhar(out var item))
            throw new InvalidOperationException("A pilha está vazia.");

        return item;
    }

    /// <summary>Versão sem exceção: devolve false se a pilha estiver vazia.</summary>
    public bool TentarDesempilhar(out T item)
    {
        if (Count == 0)
        {
            item = default!;
            return false;
        }

        item = _itens[--Count];
        _itens[Count] = default!;
        return true;
    }

    /// <summary>Devolve o item do topo sem remover. Pilha vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Topo()
    {
        if (Count == 0)
            throw new InvalidOperationException("A pilha está vazia.");

        return _itens[Count - 1];
    }
}
