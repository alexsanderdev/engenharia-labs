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
        return produtos.Where(p => p.Preco > limite);
    }

    /// <summary>
    /// Calcula quantidade, preço mínimo, máximo e médio percorrendo a fonte UMA única vez
    /// (a fonte pode ser uma consulta cara — banco, arquivo, API — ou nem poder ser repetida).
    /// Fonte vazia devolve tudo zero. A média é arredondada para 2 casas.
    /// </summary>
    public static EstatisticasCatalogo Estatisticas(IEnumerable<Produto> produtos)
    {
        var quantidade = 0;
        decimal soma = 0, minimo = 0, maximo = 0;

        foreach (var produto in produtos)
        {
            if (quantidade == 0)
            {
                minimo = maximo = produto.Preco;
            }
            else
            {
                minimo = Math.Min(minimo, produto.Preco);
                maximo = Math.Max(maximo, produto.Preco);
            }

            soma += produto.Preco;
            quantidade++;
        }

        return quantidade == 0
            ? new EstatisticasCatalogo(0, 0, 0, 0)
            : new EstatisticasCatalogo(quantidade, minimo, maximo, Math.Round(soma / quantidade, 2));
    }
}
