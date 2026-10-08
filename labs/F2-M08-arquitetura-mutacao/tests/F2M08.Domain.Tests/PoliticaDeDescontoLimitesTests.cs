using F2M08.Domain.Descontos;

namespace F2M08.Domain.Tests;

/// <summary>
/// Parte 2 (solução): testes que matam os mutantes da política de desconto.
/// Regra prática: para cada comparação, um caso EXATAMENTE no limite e um logo abaixo.
/// </summary>
public sealed class PoliticaDeDescontoLimitesTests
{
    [Theory]
    [InlineData("0", 0, "0")]
    [InlineData("499.99", 1, "0")]
    [InlineData("500", 1, "5")]
    [InlineData("999.99", 1, "5")]
    [InlineData("1000", 1, "10")]
    [InlineData("100", 9, "0")]
    [InlineData("100", 10, "2")]
    [InlineData("500", 10, "7")]
    [InlineData("1000", 10, "12")]
    public void CalcularPercentual_NosLimites_DevolveOPercentualExato(string subtotal, int unidades, string esperado)
    {
        PoliticaDeDesconto.CalcularPercentual(decimal.Parse(subtotal, System.Globalization.CultureInfo.InvariantCulture), unidades)
            .ShouldBe(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CalcularDesconto_ValorExato_AplicaOPercentualSobreOSubtotal()
    {
        PoliticaDeDesconto.CalcularDesconto(2000m, 1).ShouldBe(200m);
        PoliticaDeDesconto.CalcularDesconto(600m, 1).ShouldBe(30m);
        PoliticaDeDesconto.CalcularDesconto(1000m, 10).ShouldBe(120m);
    }

    [Fact]
    public void CalcularDesconto_MeioCentavo_ArredondaParaLongeDoZero()
    {
        // 500,10 × 5% = 25,005 → 25,01 (AwayFromZero). Com arredondamento bancário daria 25,00.
        PoliticaDeDesconto.CalcularDesconto(500.10m, 1).ShouldBe(25.01m);
    }

    [Fact]
    public void CalcularPercentual_SubtotalNegativo_LancaArgumentOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PoliticaDeDesconto.CalcularPercentual(-0.01m, 1));
    }

    [Fact]
    public void CalcularPercentual_UnidadesNegativas_LancaArgumentOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PoliticaDeDesconto.CalcularPercentual(100m, -1));
    }
}
