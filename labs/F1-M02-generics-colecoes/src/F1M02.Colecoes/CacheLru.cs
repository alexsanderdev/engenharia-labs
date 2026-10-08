using System.Diagnostics.CodeAnalysis;

namespace F1M02.Colecoes;

/// <summary>
/// Cache LRU (Least Recently Used) com capacidade fixa: ao lotar, remove o item usado há mais tempo.
/// Leitura e escrita em O(1) combinando <see cref="Dictionary{TKey, TValue}"/> (busca) com
/// <see cref="LinkedList{T}"/> (ordem de uso: início = mais recente, fim = mais antigo).
/// Não é thread-safe.
/// </summary>
public sealed class CacheLru<TChave, TValor> where TChave : notnull
{
    // TODO: declare a capacidade, um Dictionary<TChave, LinkedListNode<(TChave Chave, TValor Valor)>>
    //       e uma LinkedList<(TChave Chave, TValor Valor)>.

    /// <summary>Capacidade &lt;= 0 lança <see cref="ArgumentOutOfRangeException"/>.</summary>
    public CacheLru(int capacidade)
    {
        // TODO: valide a capacidade e guarde-a.
    }

    /// <summary>Quantidade de itens no cache.</summary>
    public int Quantidade =>
        throw new NotImplementedException("TODO: retorne a quantidade de itens");

    /// <summary>
    /// Busca o valor. Se encontrar, o item passa a ser o MAIS recente.
    /// </summary>
    public bool TentarObter(TChave chave, [MaybeNullWhen(false)] out TValor valor) =>
        throw new NotImplementedException("TODO: busque no dicionário e mova o nó para o início da lista");

    /// <summary>
    /// Insere ou atualiza. Atualizar não aumenta a quantidade e torna o item o mais recente.
    /// Inserir com o cache cheio remove antes o item MENOS recente.
    /// </summary>
    public void Definir(TChave chave, TValor valor) =>
        throw new NotImplementedException("TODO: atualize (e mova para o início) ou insira, removendo o último se estiver cheio");

    /// <summary>Chaves do mais recente para o mais antigo (snapshot, útil para depurar e testar).</summary>
    public IReadOnlyList<TChave> ChavesPorUso() =>
        throw new NotImplementedException("TODO: percorra a LinkedList do início ao fim");
}
