namespace F2M03.CodeSmells.FeatureEnvy;

/// <summary>
/// SMELL: Feature Envy. Este método mexe muito mais nos dados de <see cref="Pedido"/>, <see cref="ItemDoPedido"/>
/// e <see cref="EnderecoDeEntrega"/> do que em qualquer coisa da própria calculadora.
/// Refatoração: Move Method — cada cálculo vai para quem tem os dados, e aqui sobra só a orquestração.
/// </summary>
public sealed class CalculadoraDeFrete
{
    /// <summary>
    /// Frete = 0 se o subtotal ≥ R$ 300; senão taxa base da região + R$ 2,50 por kg (arredondado para cima)
    /// acima de 5 kg.
    /// </summary>
    public decimal Calcular(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        decimal subtotal = 0;
        decimal peso = 0;
        foreach (var item in pedido.Itens)
        {
            subtotal += item.PrecoUnitario * item.Quantidade;
            peso += item.PesoUnitarioKg * item.Quantidade;
        }

        if (subtotal >= 300m)
            return 0m;

        decimal taxaBase;
        var uf = pedido.Entrega.Uf.ToUpperInvariant();
        if (uf == "SP" || uf == "RJ" || uf == "MG" || uf == "ES")
            taxaBase = 15m;
        else if (uf == "PR" || uf == "SC" || uf == "RS")
            taxaBase = 20m;
        else
            taxaBase = 35m;

        var adicionalPeso = peso > 5m ? Math.Ceiling(peso - 5m) * 2.5m : 0m;
        return taxaBase + adicionalPeso;
    }
}
