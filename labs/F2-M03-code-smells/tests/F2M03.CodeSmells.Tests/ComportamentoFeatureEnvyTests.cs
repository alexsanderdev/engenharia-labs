using F2M03.CodeSmells.FeatureEnvy;

namespace F2M03.CodeSmells.Tests;

/// <summary>Caracterização do cálculo de frete. Nunca podem ficar vermelhos.</summary>
public class ComportamentoFeatureEnvyTests
{
    [Theory]
    [InlineData("SP", 1, 15)]
    [InlineData("rj", 5, 15)]
    [InlineData("PR", 5.1, 22.5)]
    [InlineData("sc", 0, 20)]
    [InlineData("BA", 8, 42.5)]
    public void Calcular_AbaixoDe300_TaxaDaRegiaoMaisExcedenteDePeso(string uf, decimal pesoKg, decimal esperado)
    {
        var pedido = new Pedido(new EnderecoDeEntrega(uf), [new ItemDoPedido("CANECA", 50m, 1, pesoKg)]);

        new CalculadoraDeFrete().Calcular(pedido).ShouldBe(esperado);
    }

    [Fact]
    public void Calcular_SubtotalAPartirDe300_FreteGratisMesmoPesado()
    {
        var pedido = new Pedido(new EnderecoDeEntrega("AM"),
        [
            new ItemDoPedido("CADEIRA", 120m, 2, 9m),
            new ItemDoPedido("ALMOFADA", 30m, 2, 0.5m),
        ]);

        new CalculadoraDeFrete().Calcular(pedido).ShouldBe(0m);
    }
}
