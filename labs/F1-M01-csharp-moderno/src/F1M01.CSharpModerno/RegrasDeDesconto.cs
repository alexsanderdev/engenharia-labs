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
    public static decimal PercentualPara(Pedido pedido) => pedido switch
    {
        { Itens: [] } => 0m,
        { Cliente.Vip: true, Subtotal.Valor: >= 500m } => 0.15m,
        { Cliente.Vip: true } => 0.10m,
        { Subtotal.Valor: >= 500m } => 0.05m,
        { Itens: [{ Quantidade: >= 10 }] } => 0.08m,
        { Itens: [_, _, _, ..] } => 0.03m,
        _ => 0m
    };

    /// <summary>Subtotal menos o desconto calculado por <see cref="PercentualPara"/>.</summary>
    public static Dinheiro TotalComDesconto(Pedido pedido)
    {
        var subtotal = pedido.Subtotal;
        return subtotal - subtotal.Percentual(PercentualPara(pedido));
    }
}
