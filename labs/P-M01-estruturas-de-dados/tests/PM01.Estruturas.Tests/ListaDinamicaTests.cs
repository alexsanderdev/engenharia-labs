namespace PM01.Estruturas.Tests;

public class ListaDinamicaTests
{
    [Fact]
    public void Adicionar_ItensEmOrdem_AcessaPorIndice()
    {
        var lista = new ListaDinamica<string>();

        lista.Adicionar("teclado");
        lista.Adicionar("mouse");
        lista.Adicionar("monitor");

        lista.Count.ShouldBe(3);
        lista[0].ShouldBe("teclado");
        lista[2].ShouldBe("monitor");
        lista.ToArray().ShouldBe(["teclado", "mouse", "monitor"]);
    }

    [Fact]
    public void Adicionar_AlemDaCapacidade_DobraCapacidade()
    {
        var lista = new ListaDinamica<int>();
        lista.Capacity.ShouldBe(4);

        for (var i = 0; i < 5; i++)
            lista.Adicionar(i);

        // Crescimento geométrico: 4 -> 8. É isso que torna o Add O(1) amortizado.
        lista.Capacity.ShouldBe(8);

        for (var i = 5; i < 9; i++)
            lista.Adicionar(i);

        lista.Capacity.ShouldBe(16);
        lista.Count.ShouldBe(9);
        lista[8].ShouldBe(8);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Indexador_ForaDosLimites_LancaArgumentOutOfRange(int indice)
    {
        var lista = new ListaDinamica<int>();
        lista.Adicionar(10);
        lista.Adicionar(20);
        lista.Adicionar(30);

        Should.Throw<ArgumentOutOfRangeException>(() => lista[indice]);
    }

    [Fact]
    public void Indexador_IndiceDentroDaCapacidadeMasForaDoCount_LancaArgumentOutOfRange()
    {
        var lista = new ListaDinamica<int>();
        lista.Adicionar(1); // Count = 1, Capacity = 4

        Should.Throw<ArgumentOutOfRangeException>(() => lista[2]);
    }

    [Fact]
    public void RemoverEm_DeslocaItensParaEsquerda()
    {
        var lista = new ListaDinamica<char>();
        foreach (var c in "ABCDE")
            lista.Adicionar(c);

        lista.RemoverEm(1);

        lista.Count.ShouldBe(4);
        lista.ToArray().ShouldBe(['A', 'C', 'D', 'E']);
    }

    [Fact]
    public void IndiceDe_ItemInexistente_RetornaMenosUm()
    {
        var lista = new ListaDinamica<string>();
        lista.Adicionar("a");
        lista.Adicionar("b");

        lista.IndiceDe("b").ShouldBe(1);
        lista.IndiceDe("z").ShouldBe(-1);
    }
}
