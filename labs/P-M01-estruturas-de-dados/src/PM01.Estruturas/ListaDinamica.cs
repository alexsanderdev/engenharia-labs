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
        get => throw new NotImplementedException("TODO: valide o índice contra Count (não contra Capacity) e devolva _itens[indice].");
        set => throw new NotImplementedException("TODO: valide o índice contra Count e grave em _itens[indice].");
    }

    /// <summary>
    /// Adiciona no final. Se o array estiver cheio, cria um novo com o DOBRO da capacidade
    /// e copia os itens (é isso que garante O(1) amortizado).
    /// </summary>
    public void Adicionar(T item)
    {
        throw new NotImplementedException("TODO: se Count == Capacity, dobre o array (Array.Resize); depois grave em _itens[Count] e incremente Count.");
    }

    /// <summary>
    /// Remove o item na posição informada, deslocando os seguintes uma casa para a esquerda.
    /// Limpa a última posição para não segurar referência (ajuda o GC).
    /// </summary>
    public void RemoverEm(int indice)
    {
        throw new NotImplementedException("TODO: valide o índice, desloque os itens seguintes (Array.Copy), decremente Count e limpe a última posição.");
    }

    /// <summary>Retorna o índice da primeira ocorrência do item ou -1. Busca linear O(n).</summary>
    public int IndiceDe(T item)
    {
        throw new NotImplementedException("TODO: percorra de 0 até Count - 1 comparando com EqualityComparer<T>.Default.");
    }

    /// <summary>Percorre os itens na ordem de inserção (apenas os Count primeiros).</summary>
    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException("TODO: use yield return para devolver _itens[0..Count).");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
