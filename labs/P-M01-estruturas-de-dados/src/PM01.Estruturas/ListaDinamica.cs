using System.Collections;

namespace PM01.Estruturas;

/// <summary>
/// Lista dinâmica baseada em array, equivalente simplificado de <see cref="List{T}"/>.
/// Acesso por índice O(1); <see cref="Adicionar"/> O(1) amortizado porque a capacidade
/// DOBRA quando enche (crescimento geométrico); remoção no meio O(n) por causa do deslocamento.
/// </summary>
public sealed class ListaDinamica<T> : IEnumerable<T>
{
    private const int CapacidadeInicial = 4;
    private T[] _itens = new T[CapacidadeInicial];

    /// <summary>Quantidade de itens realmente armazenados.</summary>
    public int Count { get; private set; }

    /// <summary>Tamanho do array interno (sempre &gt;= Count).</summary>
    public int Capacity => _itens.Length;

    /// <summary>
    /// Lê ou escreve o item na posição informada.
    /// Lança <see cref="ArgumentOutOfRangeException"/> se o índice for negativo ou &gt;= Count
    /// (atenção: índices entre Count e Capacity também são inválidos).
    /// </summary>
    public T this[int indice]
    {
        get
        {
            ValidarIndice(indice);
            return _itens[indice];
        }
        set
        {
            ValidarIndice(indice);
            _itens[indice] = value;
        }
    }

    /// <summary>
    /// Adiciona no final. Se o array estiver cheio, cria um novo com o DOBRO da capacidade
    /// e copia os itens (é isso que garante O(1) amortizado).
    /// </summary>
    public void Adicionar(T item)
    {
        if (Count == _itens.Length)
            Array.Resize(ref _itens, _itens.Length * 2);

        _itens[Count++] = item;
    }

    /// <summary>
    /// Remove o item na posição informada, deslocando os seguintes uma casa para a esquerda.
    /// Limpa a última posição para não segurar referência (ajuda o GC).
    /// </summary>
    public void RemoverEm(int indice)
    {
        ValidarIndice(indice);
        Array.Copy(_itens, indice + 1, _itens, indice, Count - indice - 1);
        Count--;
        _itens[Count] = default!;
    }

    /// <summary>Retorna o índice da primeira ocorrência do item ou -1. Busca linear O(n).</summary>
    public int IndiceDe(T item)
    {
        var comparador = EqualityComparer<T>.Default;
        for (var i = 0; i < Count; i++)
        {
            if (comparador.Equals(_itens[i], item))
                return i;
        }

        return -1;
    }

    /// <summary>Percorre os itens na ordem de inserção (apenas os Count primeiros).</summary>
    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return _itens[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void ValidarIndice(int indice)
    {
        if ((uint)indice >= (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(indice), indice, $"Índice deve estar entre 0 e {Count - 1}.");
    }
}
