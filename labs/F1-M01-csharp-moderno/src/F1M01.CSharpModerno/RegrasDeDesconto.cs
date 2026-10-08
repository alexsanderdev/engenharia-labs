namespace F1M01.CSharpModerno;

/// <summary>
/// Regras de desconto do OrderFlow, escritas com property, relational e list patterns.
/// </summary>
public static class RegrasDeDesconto
{
    /// <summary>
    /// Percentual de desconto (0.10m = 10%). A PRIMEIRA regra que casar vence, nesta ordem:
    /// <list type="number">
    /// <item>Pedido sem itens → 0%.</item>
    /// <item>Cliente VIP com subtotal ≥ 500 → 15%.</item>
    /// <item>Cliente VIP → 10%.</item>
    /// <item>Subtotal ≥ 500 → 5%.</item>
    /// <item>Atacado: exatamente UM item com quantidade ≥ 10 → 8%.</item>
    /// <item>Três ou mais itens → 3%.</item>
    /// <item>Qualquer outro caso → 0%.</item>
    /// </list>
    /// </summary>
    public static decimal PercentualPara(Pedido pedido) =>
        throw new NotImplementedException("TODO: escreva uma switch expression com property, relational e list patterns (ex.: { Itens: [] }, { Cliente.Vip: true }, { Subtotal.Valor: >= 500m })");

    /// <summary>Subtotal menos o desconto calculado por <see cref="PercentualPara"/>.</summary>
    public static Dinheiro TotalComDesconto(Pedido pedido) =>
        throw new NotImplementedException("TODO: subtotal menos subtotal.Percentual(PercentualPara(pedido))");
}
