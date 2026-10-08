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
        throw new NotImplementedException("TODO: se cheia, cresça (copiando a partir de _cabeca, com %, e zerando _cabeca); grave em (_cabeca + Count) % Capacity.");
    }

    /// <summary>Remove e devolve o primeiro da fila. Fila vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Desenfileirar()
    {
        throw new NotImplementedException("TODO: leia _itens[_cabeca], limpe a posição, avance _cabeca com % Capacity e decremente Count.");
    }

    /// <summary>Devolve o primeiro da fila sem remover. Fila vazia: <see cref="InvalidOperationException"/>.</summary>
    public T Espiar()
    {
        throw new NotImplementedException("TODO: devolva _itens[_cabeca] ou lance InvalidOperationException se vazia.");
    }

    /// <summary>Percorre da cabeça até a cauda (ordem FIFO).</summary>
    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException("TODO: yield return _itens[(_cabeca + i) % Capacity] para i de 0 até Count - 1.");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
