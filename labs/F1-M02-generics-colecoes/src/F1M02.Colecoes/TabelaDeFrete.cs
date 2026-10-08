using System.Collections.Frozen;

namespace F1M02.Colecoes;

/// <summary>
/// Tabela de frete por UF: montada uma vez na inicialização e lida milhões de vezes.
/// Caso clássico para <see cref="FrozenDictionary{TKey, TValue}"/>: criação mais cara, leitura mais rápida,
/// e imutável (alterar o dicionário de origem depois não muda a tabela).
/// </summary>
public sealed class TabelaDeFrete
{
    /// <summary>
    /// Cria a tabela a partir dos valores por UF, sem diferenciar maiúsculas (ex.: "sp" == "SP").
    /// </summary>
    public TabelaDeFrete(IEnumerable<KeyValuePair<string, decimal>> valoresPorUf)
    {
        ArgumentNullException.ThrowIfNull(valoresPorUf);
        Tabela = valoresPorUf.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>A tabela imutável (deve ser um <see cref="FrozenDictionary{TKey, TValue}"/>).</summary>
    public IReadOnlyDictionary<string, decimal> Tabela { get; }

    /// <summary>Valor do frete para a UF. UF desconhecida lança <see cref="KeyNotFoundException"/>.</summary>
    public decimal ValorPara(string uf) =>
        Tabela.TryGetValue(uf, out var valor)
            ? valor
            : throw new KeyNotFoundException($"Não entregamos para a UF '{uf}'.");

    /// <summary>Versão sem exceção: retorna <c>false</c> para UF desconhecida.</summary>
    public bool TentarObter(string uf, out decimal valor) => Tabela.TryGetValue(uf, out valor);
}
