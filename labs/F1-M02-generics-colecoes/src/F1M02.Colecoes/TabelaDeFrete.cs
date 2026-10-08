namespace F1M02.Colecoes;

// Dica: using System.Collections.Frozen; para ToFrozenDictionary.

/// <summary>
/// Tabela de frete por UF: montada uma vez na inicialização e lida milhões de vezes.
/// Caso clássico para <see cref="System.Collections.Frozen.FrozenDictionary{TKey, TValue}"/>: criação mais cara,
/// leitura mais rápida, e imutável (alterar o dicionário de origem depois não muda a tabela).
/// </summary>
public sealed class TabelaDeFrete
{
    /// <summary>
    /// Cria a tabela a partir dos valores por UF, sem diferenciar maiúsculas (ex.: "sp" == "SP").
    /// </summary>
    public TabelaDeFrete(IEnumerable<KeyValuePair<string, decimal>> valoresPorUf)
    {
        // TODO: Tabela = valoresPorUf.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        throw new NotImplementedException("TODO: monte a Tabela como FrozenDictionary sem diferenciar maiúsculas");
    }

    /// <summary>A tabela imutável (deve ser um FrozenDictionary).</summary>
    public IReadOnlyDictionary<string, decimal> Tabela { get; }

    /// <summary>Valor do frete para a UF. UF desconhecida lança <see cref="KeyNotFoundException"/>.</summary>
    public decimal ValorPara(string uf) =>
        throw new NotImplementedException("TODO: retorne o valor ou lance KeyNotFoundException");

    /// <summary>Versão sem exceção: retorna <c>false</c> para UF desconhecida.</summary>
    public bool TentarObter(string uf, out decimal valor) =>
        throw new NotImplementedException("TODO: use TryGetValue");
}
