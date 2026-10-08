using F2M08.Domain.Descontos;

namespace F2M08.Domain.Tests;

/// <summary>Suíte fraca da política de desconto: nenhum teste toca os limites (500, 1.000, 10 unidades).</summary>
public sealed class PoliticaDeDescontoTests
{
    [Fact]
    public void CalcularDesconto_SubtotalPequeno_SemDesconto()
    {
        PoliticaDeDesconto.CalcularDesconto(100m, 1).ShouldBe(0m);
    }

    [Fact]
    public void CalcularDesconto_SubtotalGrande_TemDesconto()
    {
        PoliticaDeDesconto.CalcularDesconto(2000m, 1).ShouldBeGreaterThan(0m);
    }

    [Fact]
    public void CalcularPercentual_MuitasUnidades_AumentaOPercentual()
    {
        PoliticaDeDesconto.CalcularPercentual(100m, 50)
            .ShouldBeGreaterThan(PoliticaDeDesconto.CalcularPercentual(100m, 1));
    }
}
