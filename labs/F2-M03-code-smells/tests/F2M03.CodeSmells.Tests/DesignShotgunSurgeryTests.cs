using F2M03.CodeSmells.ShotgunSurgery;

namespace F2M03.CodeSmells.Tests;

/// <summary>Design: a regra centralizada em PoliticaDeQuantidade. Começam vermelhos.</summary>
public class DesignShotgunSurgeryTests
{
    [Fact]
    public void PoliticaPadrao_Permite1a10EExplicaORecusado()
    {
        var politica = PoliticaDeQuantidade.Padrao;

        politica.MaximoPorItem.ShouldBe(10);
        politica.Permite(0).ShouldBeFalse();
        politica.Permite(1).ShouldBeTrue();
        politica.Permite(10).ShouldBeTrue();
        politica.Permite(11).ShouldBeFalse();
        Should.Throw<ArgumentOutOfRangeException>(() => politica.Validar(11)).Message.ShouldContain("entre 1 e 10");
    }

    [Fact]
    public void MudarAPoliticaEmUmLugar_AfetaCarrinhoCheckoutEImportador()
    {
        var politica = new PoliticaDeQuantidade(20);

        var carrinho = new Carrinho(politica);
        carrinho.Adicionar("CANECA", 15);
        carrinho.Itens["CANECA"].ShouldBe(15);
        Should.Throw<ArgumentOutOfRangeException>(() => carrinho.Adicionar("CANECA", 6)).Message.ShouldContain("entre 1 e 20");

        new Checkout(politica).PodeFinalizar(new Dictionary<string, int> { ["CANECA"] = 15 }).ShouldBeTrue();
        new ImportadorDePedidosCsv(politica).Importar(["CANECA;15", "CAMISA;21"]).ShouldBe([("CANECA", 15)]);
    }
}
