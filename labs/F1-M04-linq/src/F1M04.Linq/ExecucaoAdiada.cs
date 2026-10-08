namespace F1M04.Linq;

/// <summary>
/// Exercícios sobre execução adiada (deferred execution) e múltipla enumeração.
/// </summary>
public static class ExecucaoAdiada
{
    /// <summary>
    /// Devolve, de forma ADIADA, os produtos com preço acima do limite.
    /// Chamar o método não pode enumerar a fonte; a filtragem só acontece quando alguém
    /// percorrer o resultado (e reflete o estado da fonte naquele momento).
    /// </summary>
    public static IEnumerable<Produto> AcimaDe(IEnumerable<Produto> produtos, decimal limite)
    {
        throw new NotImplementedException("TODO: devolva a consulta SEM materializar (nada de ToList aqui)");
    }

    /// <summary>
    /// Calcula quantidade, preço mínimo, máximo e médio percorrendo a fonte UMA única vez
    /// (a fonte pode ser uma consulta cara — banco, arquivo, API — ou nem poder ser repetida).
    /// Fonte vazia devolve tudo zero. A média é arredondada para 2 casas.
    /// </summary>
    public static EstatisticasCatalogo Estatisticas(IEnumerable<Produto> produtos)
    {
        throw new NotImplementedException("TODO: um único foreach acumulando contagem, soma, mínimo e máximo (Count()+Min()+Max()+Average() = 4 enumerações)");
    }
}
