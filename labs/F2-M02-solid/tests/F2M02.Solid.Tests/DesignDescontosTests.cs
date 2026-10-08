using F2M02.Solid.Descontos;
using F2M02.Solid.Dominio;

namespace F2M02.Solid.Tests;

/// <summary>OCP + LSP: políticas de desconto como Strategy, todas cumprindo o mesmo contrato.</summary>
public class DesignDescontosTests
{
    [Fact]
    public void DescontoPercentual_20PorCentoDe200_Da40()
    {
        new DescontoPercentual("BLACKFRIDAY", 0.20m).CalcularDesconto(200m).ShouldBe(40m);
    }

    [Fact]
    public void DescontoPercentual_ArredondaParaDuasCasas()
    {
        // 10% de 100,05 = 10,005 → 10,01
        new DescontoPercentual("X", 0.10m).CalcularDesconto(100.05m).ShouldBe(10.01m);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void DescontoPercentual_PercentualForaDeZeroAUm_Lanca(double percentual)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new DescontoPercentual("X", (decimal)percentual));
    }

    [Fact]
    public void DescontoPercentualComTeto_NaoPassaDoTeto()
    {
        var politica = new DescontoPercentualComTeto("PRIMEIRACOMPRA", 0.10m, 50m);

        politica.CalcularDesconto(150m).ShouldBe(15m);
        politica.CalcularDesconto(750m).ShouldBe(50m);
    }

    [Fact]
    public void DescontoFixo_NuncaUltrapassaOSubtotal()
    {
        var politica = new DescontoFixo("BEMVINDO30", 30m);

        politica.CalcularDesconto(100m).ShouldBe(30m);
        politica.CalcularDesconto(20m).ShouldBe(20m);
    }

    public static TheoryData<string> NomesDasPoliticas => ["percentual", "teto", "fixo"];

    private static IPoliticaDeDesconto CriarPolitica(string nome) => nome switch
    {
        "percentual" => new DescontoPercentual("P", 0.20m),
        "teto" => new DescontoPercentualComTeto("T", 0.10m, 50m),
        "fixo" => new DescontoFixo("F", 30m),
        _ => throw new ArgumentOutOfRangeException(nameof(nome)),
    };

    /// <summary>LSP: o MESMO teste vale para qualquer implementação — quem usa a interface não precisa saber qual recebeu.</summary>
    [Theory]
    [MemberData(nameof(NomesDasPoliticas))]
    public void TodaPolitica_CumpreOContrato_DescontoEntreZeroESubtotal(string nome)
    {
        var politica = CriarPolitica(nome);

        foreach (var subtotal in new[] { 0m, 0.01m, 10m, 29.99m, 100m, 1000m })
        {
            var desconto = politica.CalcularDesconto(subtotal);
            desconto.ShouldBeGreaterThanOrEqualTo(0m, $"{nome} com subtotal {subtotal}");
            desconto.ShouldBeLessThanOrEqualTo(subtotal, $"{nome} com subtotal {subtotal}");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Catalogo_SemCupom_DevolveSemDesconto(string? cupom)
    {
        CatalogoDePoliticasDeDesconto.Padrao().Obter(cupom).ShouldBeSameAs(SemDesconto.Instancia);
    }

    [Fact]
    public void Catalogo_CupomComCaixaEEspacosDiferentes_Encontra()
    {
        CatalogoDePoliticasDeDesconto.Padrao().Obter(" blackFriday ").Cupom.ShouldBe("BLACKFRIDAY");
    }

    [Fact]
    public void Catalogo_CupomDesconhecido_Lanca()
    {
        Should.Throw<PedidoInvalidoException>(() => CatalogoDePoliticasDeDesconto.Padrao().Obter("NATAL"))
            .Message.ShouldBe("Cupom inválido: NATAL");
    }

    /// <summary>OCP: cupom novo entra registrando uma classe nova — o catálogo não muda.</summary>
    [Fact]
    public void Catalogo_PoliticaNovaRegistrada_FuncionaSemAlterarOCatalogo()
    {
        var catalogo = new CatalogoDePoliticasDeDesconto([new DescontoDeNatal()]);

        catalogo.Obter("NATAL").CalcularDesconto(100m).ShouldBe(25m);
    }

    private sealed class DescontoDeNatal : IPoliticaDeDesconto
    {
        public string Cupom => "NATAL";

        public decimal CalcularDesconto(decimal subtotal) => Math.Min(25m, subtotal);
    }
}
