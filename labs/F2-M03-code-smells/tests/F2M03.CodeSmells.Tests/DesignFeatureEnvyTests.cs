using F2M03.CodeSmells.FeatureEnvy;

namespace F2M03.CodeSmells.Tests;

/// <summary>Design: os cálculos moram em quem tem os dados (Move Method). Começam vermelhos.</summary>
public class DesignFeatureEnvyTests
{
    [Fact]
    public void Pedido_SubtotalPesoEFreteGratis_CalculadosPeloProprioPedido()
    {
        var item = new ItemDoPedido("CADEIRA", 120m, 2, 9m);
        var pedido = new Pedido(new EnderecoDeEntrega("SP"), [item, new ItemDoPedido("ALMOFADA", 29.99m, 2, 0.5m)]);

        item.Subtotal().ShouldBe(240m);
        item.PesoTotalKg().ShouldBe(18m);
        pedido.Subtotal().ShouldBe(299.98m);
        pedido.PesoTotalKg().ShouldBe(19m);
        pedido.TemFreteGratis().ShouldBeFalse();
    }

    [Theory]
    [InlineData("SP", 15)]
    [InlineData("es", 15)]
    [InlineData("RS", 20)]
    [InlineData("pe", 35)]
    public void Endereco_TaxaBaseDeFrete_DependeDaRegiao(string uf, decimal esperado)
    {
        new EnderecoDeEntrega(uf).TaxaBaseDeFrete().ShouldBe(esperado);
    }
}
