using F2M03.CodeSmells.ShotgunSurgery;

namespace F2M03.CodeSmells.Tests;

/// <summary>Caracterização da regra "1 a 10 unidades por item", hoje espalhada. Nunca podem ficar vermelhos.</summary>
public class ComportamentoShotgunSurgeryTests
{
    [Fact]
    public void Carrinho_Adicionar_AceitaAte10PorSkuERecusaOResto()
    {
        var carrinho = new Carrinho();
        carrinho.Adicionar("CANECA", 6);
        carrinho.Adicionar("CANECA", 4);
        carrinho.Itens["CANECA"].ShouldBe(10);

        Should.Throw<ArgumentOutOfRangeException>(() => carrinho.Adicionar("CANECA", 1)).Message.ShouldContain("entre 1 e 10");
        Should.Throw<ArgumentOutOfRangeException>(() => carrinho.Adicionar("CAMISA", 0));
        Should.Throw<ArgumentOutOfRangeException>(() => carrinho.Adicionar("CAMISA", 11));
        carrinho.Itens.ContainsKey("CAMISA").ShouldBeFalse();
    }

    [Fact]
    public void CheckoutEImportador_AplicamAMesmaRegraDe1a10()
    {
        var checkout = new Checkout();
        checkout.PodeFinalizar(new Dictionary<string, int> { ["A"] = 1, ["B"] = 10 }).ShouldBeTrue();
        checkout.PodeFinalizar(new Dictionary<string, int> { ["A"] = 11 }).ShouldBeFalse();
        checkout.PodeFinalizar(new Dictionary<string, int>()).ShouldBeFalse();

        var importados = new ImportadorDePedidosCsv().Importar(["A;1", "B;10", "C;11", "D;0", "E;abc", ";3", "F", " G ; 2 "]);
        importados.ShouldBe([("A", 1), ("B", 10), ("G", 2)]);
    }
}
