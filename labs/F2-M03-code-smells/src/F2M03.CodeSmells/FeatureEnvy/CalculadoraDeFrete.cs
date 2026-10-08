namespace F2M03.CodeSmells.FeatureEnvy;

/// <summary>
/// SMELL (antes): Feature Envy. O método original lia os itens, somava preço e peso e decidia a região da UF:
/// usava muito mais os dados de <see cref="Pedido"/> e <see cref="EnderecoDeEntrega"/> do que os próprios.
/// Depois de "mover método", a calculadora só orquestra.
/// </summary>
public sealed class CalculadoraDeFrete
{
    /// <summary>Peso incluso na taxa base; cada kg (ou fração) acima disso custa <see cref="ValorPorKgExcedente"/>.</summary>
    public const decimal PesoInclusoKg = 5m;
    public const decimal ValorPorKgExcedente = 2.5m;

    /// <summary>
    /// Frete = 0 se o subtotal ≥ R$ 300; senão taxa base da região + R$ 2,50 por kg (arredondado para cima)
    /// acima de 5 kg.
    /// </summary>
    public decimal Calcular(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        if (pedido.TemFreteGratis())
            return 0m;

        var excedenteKg = Math.Max(0m, pedido.PesoTotalKg() - PesoInclusoKg);
        return pedido.Entrega.TaxaBaseDeFrete() + Math.Ceiling(excedenteKg) * ValorPorKgExcedente;
    }
}
