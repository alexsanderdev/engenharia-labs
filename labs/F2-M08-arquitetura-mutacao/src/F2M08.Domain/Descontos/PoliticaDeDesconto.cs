namespace F2M08.Domain.Descontos;

/// <summary>
/// Desconto progressivo do OrderFlow:
/// <list type="bullet">
/// <item>subtotal a partir de 500,00 → 5%; a partir de 1.000,00 → 10%;</item>
/// <item>pedido com 10 unidades ou mais ganha +2 pontos percentuais (cumulativo);</item>
/// <item>o valor do desconto é arredondado para 2 casas (meio para longe do zero).</item>
/// </list>
/// Regra cheia de limites (>=, >) e constantes: o lugar perfeito para mutantes sobreviverem.
/// </summary>
public static class PoliticaDeDesconto
{
    public const decimal FaixaPrata = 500m;
    public const decimal FaixaOuro = 1000m;
    public const int UnidadesParaBonusDeVolume = 10;

    /// <summary>Percentual (0 a 12) aplicável ao subtotal e à quantidade total de unidades.</summary>
    public static decimal CalcularPercentual(decimal subtotal, int unidades)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(subtotal);
        ArgumentOutOfRangeException.ThrowIfNegative(unidades);

        var percentual = subtotal switch
        {
            >= FaixaOuro => 10m,
            >= FaixaPrata => 5m,
            _ => 0m,
        };

        if (unidades >= UnidadesParaBonusDeVolume)
            percentual += 2m;

        return percentual;
    }

    /// <summary>Valor do desconto em reais, arredondado para 2 casas.</summary>
    public static decimal CalcularDesconto(decimal subtotal, int unidades)
    {
        var percentual = CalcularPercentual(subtotal, unidades);
        return Math.Round(subtotal * percentual / 100m, 2, MidpointRounding.AwayFromZero);
    }
}
