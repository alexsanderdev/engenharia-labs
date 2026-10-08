namespace F2M01.CleanCode;

/// <summary>
/// Regras de frete do OrderFlow. Antes eram três ifs aninhados, números soltos e um comentário
/// que dizia "frete grátis acima de 200" quando o código usava 300.
/// </summary>
public sealed class RegrasDeFrete
{
    /// <summary>A partir deste valor (já com desconto), a entrega padrão é grátis.</summary>
    public const decimal ValorMinimoParaFreteGratis = 300m;

    // TODO: dê nome aos outros números mágicos do legado (15, 25, 40, 2) com constantes.

    /// <summary>
    /// Frete por região (SP 15; RJ/MG/ES/PR/SC/RS 25; demais 40). Entrega padrão é grátis quando
    /// <paramref name="valorDosProdutos"/> ≥ <see cref="ValorMinimoParaFreteGratis"/>.
    /// Entrega expressa custa o dobro e nunca é grátis. A UF é normalizada (trim + maiúsculas).
    /// </summary>
    public decimal Calcular(string uf, decimal valorDosProdutos, ModalidadeDeEntrega modalidade)
    {
        // Dica: guard clause para a expressa primeiro; depois um método privado FretePorRegiao(uf).
        throw new NotImplementedException("TODO: extraia a regra de frete de PedidoUtil.Calc, sem ifs aninhados nem números mágicos");
    }
}
