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
        throw new NotImplementedException("TODO: igual ao Adicionar da ListaDinamica: dobre se cheio e grave em _itens[Count++].");
    }

    /// <summary>Remove e devolve o item do topo. Pilha vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Desempilhar()
    {
        throw new NotImplementedException("TODO: reutilize TentarDesempilhar e lance InvalidOperationException se ela devolver false.");
    }

    /// <summary>Versão sem exceção: devolve false se a pilha estiver vazia.</summary>
    public bool TentarDesempilhar(out T item)
    {
        throw new NotImplementedException("TODO: se vazia, item = default e false; senão devolva _itens[--Count] e limpe a posição.");
    }

    /// <summary>Devolve o item do topo sem remover. Pilha vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Topo()
    {
        throw new NotImplementedException("TODO: devolva _itens[Count - 1] ou lance InvalidOperationException se vazia.");
    }
}
