using MP2.OrderCalc.Dominio;

namespace MP2.OrderCalc.Tests;

public class MoneyTests
{
    [Fact]
    public void Arredondar_MeioCentavo_UsaArredondamentoBancario()
    {
        Money.Reais(4.485m).Arredondar().ShouldBe(Money.Reais(4.48m));
        Money.Reais(4.475m).Arredondar().ShouldBe(Money.Reais(4.48m));
    }

    [Fact]
    public void Percentual_AplicaFracaoEArredonda()
    {
        Money.Reais(89.70m).Percentual(0.05m).ShouldBe(Money.Reais(4.48m));
    }

    [Fact]
    public void PontosPercentuais_DezPorCento()
    {
        Money.Reais(699.80m).PontosPercentuais(10).ShouldBe(Money.Reais(69.98m));
    }

    [Fact]
    public void Operadores_SomaSubtracaoEComparacao()
    {
        var a = Money.Reais(10m);
        var b = Money.Reais(2.5m);

        (a + b).ShouldBe(Money.Reais(12.5m));
        (a - b).ShouldBe(Money.Reais(7.5m));
        (a > b).ShouldBeTrue();
        Money.Min(a, b).ShouldBe(b);
    }

    [Fact]
    public void Igualdade_IgnoraEscalaDoDecimal()
    {
        Money.Reais(10m).ShouldBe(Money.Reais(10.00m));
    }
}

public class QuantidadeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Criar_ZeroOuNegativa_Lanca(int valor)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Quantidade.Criar(valor));
        Quantidade.TentarCriar(valor, out _).ShouldBeFalse();
    }

    [Fact]
    public void Multiplicacao_PrecoVezesQuantidade()
    {
        (Money.Reais(29.90m) * Quantidade.Criar(3)).ShouldBe(Money.Reais(89.70m));
    }
}

public class UfTests
{
    [Theory]
    [InlineData("sp", "SP", true, false)]
    [InlineData("RS", "RS", false, true)]
    [InlineData("XX", "XX", false, false)]
    public void Ler_DuasLetras_NormalizaEClassificaRegiao(string texto, string sigla, bool sudeste, bool sul)
    {
        var uf = Uf.Ler(texto);

        uf.Sigla.ShouldBe(sigla);
        uf.EhSudeste.ShouldBe(sudeste);
        uf.EhSul.ShouldBe(sul);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("S")]
    [InlineData("SPX")]
    public void Ler_TamanhoErrado_PedidoInvalido(string? texto)
    {
        Should.Throw<PedidoInvalidoException>(() => Uf.Ler(texto)).Message.ShouldBe("UF inválida");
    }
}

public class ItemPedidoTests
{
    private static readonly ProdutoCatalogo Cabo = new("CB-008", Money.Reais(29.90m), 0.05, false, true, 500);

    [Theory]
    [InlineData(9, "0")]
    [InlineData(10, "14.95")]
    [InlineData(49, "73.26")]
    [InlineData(50, "149.50")]
    public void DescontoPorVolume_Faixas(int quantidade, string esperado)
    {
        var item = new ItemPedido(Cabo, Quantidade.Criar(quantidade));

        item.DescontoPorVolume.ShouldBe(Money.Reais(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Descricao_FormatoDoLegado()
    {
        new ItemPedido(Cabo, Quantidade.Criar(10)).Descricao.ShouldBe("CB-008 x10 = 284.05");
    }
}

public class RegrasTests
{
    [Theory]
    [InlineData(2026, 11, 23, false)]
    [InlineData(2026, 11, 24, true)]
    [InlineData(2026, 11, 30, true)]
    [InlineData(2026, 12, 1, false)]
    public void EhSemanaBlackFriday_Limites(int ano, int mes, int dia, bool esperado)
    {
        RegrasDeDesconto.EhSemanaBlackFriday(new DateTime(ano, mes, dia)).ShouldBe(esperado);
    }

    [Fact]
    public void AplicarTeto_AcimaDe30Porcento_LimitaEAvisa()
    {
        var mensagens = new List<string>();

        var desconto = RegrasDeDesconto.AplicarTeto(Money.Reais(400m), Money.Reais(1000m), mensagens);

        desconto.ShouldBe(Money.Reais(300m));
        mensagens.ShouldBe(["Desconto limitado a 30%"]);
    }

    [Theory]
    [InlineData("SP", 15)]
    [InlineData("MG", 20)]
    [InlineData("SC", 25)]
    [InlineData("AM", 35)]
    public void Frete_TabelaPorRegiao(string uf, int esperado)
    {
        RegrasDeFrete.Calcular(Uf.Ler(uf), 1, TipoEntrega.Normal).ShouldBe(Money.Reais(esperado));
    }

    [Fact]
    public void Frete_PesoExcedenteEExpresso()
    {
        // BA: 35 + ceil(18,5 - 10) * 2,50 = 57,50; expresso x1,8 = 103,50
        RegrasDeFrete.Calcular(Uf.Ler("BA"), 18.5, TipoEntrega.Expresso).ShouldBe(Money.Reais(103.50m));
    }

    [Fact]
    public void Prazo_PulaFimDeSemana()
    {
        var sexta = new DateTime(2026, 3, 13, 18, 0, 0);

        CalendarioDeEntrega.Prazo(sexta, Uf.Ler("SP"), TipoEntrega.Expresso).ShouldBe(new DateTime(2026, 3, 17));
    }

    [Fact]
    public void Imposto_LivroIsentoFicaForaDaBase_AliquotaPorUf()
    {
        RegrasDeImposto.Aliquota(Uf.Ler("RJ")).ShouldBe(0.20m);
        RegrasDeImposto.Calcular(Money.Reais(100m), Uf.Ler("SP")).ShouldBe(Money.Reais(18m));
    }
}
