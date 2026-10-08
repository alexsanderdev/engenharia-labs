using System.Collections;

namespace PM01.Estruturas;

/// <summary>
/// Fila FIFO sobre um array circular (ring buffer), equivalente simplificado de <see cref="Queue{T}"/>.
/// Guarda o índice da cabeça e a quantidade; a cauda é <c>(cabeca + Count) % Capacity</c>.
/// Enfileirar e desenfileirar são O(1) sem deslocar itens: as posições liberadas são reaproveitadas.
/// </summary>
public sealed class FilaCircular<T> : IEnumerable<T>
{
    private T[] _itens;
    private int _cabeca;

    /// <summary>Cria a fila com a capacidade inicial informada (mínimo 1).</summary>
    public FilaCircular(int capacidadeInicial = 4)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacidadeInicial, 1);
        _itens = new T[capacidadeInicial];
    }

    /// <summary>Quantidade de itens na fila.</summary>
    public int Count { get; private set; }

    /// <summary>Tamanho do array interno.</summary>
    public int Capacity => _itens.Length;

    /// <summary>
    /// Coloca o item no fim da fila. Se estiver cheia, cresce para o dobro
    /// copiando os itens NA ORDEM LÓGICA (da cabeça até a cauda) para o início do novo array.
    /// </summary>
    public void Enfileirar(T item)
    {
        if (Count == _itens.Length)
            Crescer();

        var cauda = (_cabeca + Count) % _itens.Length;
        _itens[cauda] = item;
        Count++;
    }

    /// <summary>Remove e devolve o primeiro da fila. Fila vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Desenfileirar()
    {
        if (Count == 0)
            throw new InvalidOperationException("A fila está vazia.");

        var item = _itens[_cabeca];
        _itens[_cabeca] = default!;
        _cabeca = (_cabeca + 1) % _itens.Length;
        Count--;
        return item;
    }

    /// <summary>Devolve o primeiro da fila sem remover. Fila vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Espiar()
    {
        if (Count == 0)
            throw new InvalidOperationException("A fila está vazia.");

        return _itens[_cabeca];
    }

    /// <summary>Percorre da cabeça até a cauda (ordem FIFO).</summary>
    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return _itens[(_cabeca + i) % _itens.Length];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Crescer()
    {
        var novo = new T[_itens.Length * 2];
        for (var i = 0; i < Count; i++)
            novo[i] = _itens[(_cabeca + i) % _itens.Length];

        _itens = novo;
        _cabeca = 0;
    }
}
