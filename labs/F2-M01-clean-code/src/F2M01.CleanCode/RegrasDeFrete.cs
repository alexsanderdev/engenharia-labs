namespace F2M01.CleanCode;

/// <summary>
/// Regras de frete do OrderFlow. Antes eram três ifs aninhados, números soltos e um comentário
/// que dizia "frete grátis acima de 200" quando o código usava 300.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Instância de propósito: a classe é injetada e pode ganhar dependências depois.")]
public sealed class RegrasDeFrete
{
    /// <summary>A partir deste valor (já com desconto), a entrega padrão é grátis.</summary>
    public const decimal ValorMinimoParaFreteGratis = 300m;

    public const decimal FreteParaSaoPaulo = 15m;
    public const decimal FreteParaSulESudeste = 25m;
    public const decimal FreteParaDemaisRegioes = 40m;
    public const decimal MultiplicadorDaEntregaExpressa = 2m;

    private static readonly HashSet<string> UfsDoSulESudesteForaDeSp = ["RJ", "MG", "ES", "PR", "SC", "RS"];

    /// <summary>
    /// Frete por região (SP 15; RJ/MG/ES/PR/SC/RS 25; demais 40). Entrega padrão é grátis quando
    /// <paramref name="valorDosProdutos"/> ≥ <see cref="ValorMinimoParaFreteGratis"/>.
    /// Entrega expressa custa o dobro e nunca é grátis. A UF é normalizada (trim + maiúsculas).
    /// </summary>
    public decimal Calcular(string uf, decimal valorDosProdutos, ModalidadeDeEntrega modalidade)
    {
        if (modalidade == ModalidadeDeEntrega.Expressa)
        {
            return FretePorRegiao(uf) * MultiplicadorDaEntregaExpressa;
        }

        return valorDosProdutos >= ValorMinimoParaFreteGratis ? 0m : FretePorRegiao(uf);
    }

    private static decimal FretePorRegiao(string uf)
    {
        var ufNormalizada = uf.Trim().ToUpperInvariant();

        if (ufNormalizada == "SP")
        {
            return FreteParaSaoPaulo;
        }

        return UfsDoSulESudesteForaDeSp.Contains(ufNormalizada) ? FreteParaSulESudeste : FreteParaDemaisRegioes;
    }
}
