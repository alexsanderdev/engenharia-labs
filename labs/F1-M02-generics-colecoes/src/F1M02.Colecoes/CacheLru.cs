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
    private readonly int _capacidade;
    private readonly Dictionary<TChave, LinkedListNode<(TChave Chave, TValor Valor)>> _mapa;
    private readonly LinkedList<(TChave Chave, TValor Valor)> _ordem = new();

    /// <summary>Capacidade &lt;= 0 lança <see cref="ArgumentOutOfRangeException"/>.</summary>
    public CacheLru(int capacidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacidade);
        _capacidade = capacidade;
        _mapa = new Dictionary<TChave, LinkedListNode<(TChave, TValor)>>(capacidade);
    }

    /// <summary>Quantidade de itens no cache.</summary>
    public int Quantidade => _mapa.Count;

    /// <summary>
    /// Busca o valor. Se encontrar, o item passa a ser o MAIS recente.
    /// </summary>
    public bool TentarObter(TChave chave, [MaybeNullWhen(false)] out TValor valor)
    {
        if (_mapa.TryGetValue(chave, out var no))
        {
            MoverParaInicio(no);
            valor = no.Value.Valor;
            return true;
        }
        valor = default;
        return false;
    }

    /// <summary>
    /// Insere ou atualiza. Atualizar não aumenta a quantidade e torna o item o mais recente.
    /// Inserir com o cache cheio remove antes o item MENOS recente.
    /// </summary>
    public void Definir(TChave chave, TValor valor)
    {
        if (_mapa.TryGetValue(chave, out var existente))
        {
            existente.Value = (chave, valor);
            MoverParaInicio(existente);
            return;
        }

        if (_mapa.Count == _capacidade)
        {
            var maisAntigo = _ordem.Last!;
            _ordem.RemoveLast();
            _mapa.Remove(maisAntigo.Value.Chave);
        }

        _mapa[chave] = _ordem.AddFirst((chave, valor));
    }

    /// <summary>Chaves do mais recente para o mais antigo (snapshot, útil para depurar e testar).</summary>
    public IReadOnlyList<TChave> ChavesPorUso() => [.. _ordem.Select(n => n.Chave)];

    private void MoverParaInicio(LinkedListNode<(TChave Chave, TValor Valor)> no)
    {
        if (no == _ordem.First) return;
        _ordem.Remove(no);
        _ordem.AddFirst(no);
    }
}
