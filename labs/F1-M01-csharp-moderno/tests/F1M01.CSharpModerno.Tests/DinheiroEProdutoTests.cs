namespace F1M01.CSharpModerno.Tests;

// Passo 1 — Dinheiro: value object com igualdade por valor.
public class DinheiroTests
{
    [Fact]
    public void Dinheiro_MesmoValorEMoeda_SaoIguaisPorValor()
    {
        var a = new Dinheiro(10m, "brl");
        var b = Dinheiro.Reais(10.00m);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        new HashSet<Dinheiro> { a, b }.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(10.005, 10.01)]
    [InlineData(10.004, 10.00)]
    [InlineData(0.125, 0.13)]
    public void Dinheiro_ArredondaParaDuasCasasAwayFromZero(decimal entrada, decimal esperado)
    {
        Dinheiro.Reais(entrada).Valor.ShouldBe(esperado);
    }

    [Fact]
    public void Dinheiro_ValoresInvalidos_LancamExcecao()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Dinheiro.Reais(-0.01m));
        Should.Throw<ArgumentException>(() => new Dinheiro(1m, " "));
    }

    [Fact]
    public void Dinheiro_Operadores_SomamSubtraemEMultiplicam()
    {
        (Dinheiro.Reais(10m) + Dinheiro.Reais(5.5m)).ShouldBe(Dinheiro.Reais(15.5m));
        (Dinheiro.Reais(10m) - Dinheiro.Reais(2.5m)).ShouldBe(Dinheiro.Reais(7.5m));
        (Dinheiro.Reais(3.33m) * 3).ShouldBe(Dinheiro.Reais(9.99m));
        Dinheiro.Reais(200m).Percentual(0.15m).ShouldBe(Dinheiro.Reais(30m));
        Dinheiro.Reais(15.5m).ToString().ShouldBe("BRL 15.50");
    }

    [Fact]
    public void Dinheiro_OperacoesInvalidas_LancamInvalidOperation()
    {
        Should.Throw<InvalidOperationException>(() => Dinheiro.Reais(1m) + new Dinheiro(1m, "USD"));
        Should.Throw<InvalidOperationException>(() => Dinheiro.Reais(1m) - Dinheiro.Reais(2m));
    }
}

// Passo 2 — Produto: required/init, with e validação no init (field keyword).
public class ProdutoTests
{
    private static Produto NovoProduto(string nome = "X-Burger", decimal preco = 30m) =>
        new() { Nome = nome, Preco = Dinheiro.Reais(preco) };

    [Fact]
    public void Produto_ComPreco_CriaCopiaSemAlterarOriginal()
    {
        var original = NovoProduto();

        var reajustado = original.ComPreco(Dinheiro.Reais(35m));

        reajustado.Preco.ShouldBe(Dinheiro.Reais(35m));
        reajustado.Id.ShouldBe(original.Id);
        original.Preco.ShouldBe(Dinheiro.Reais(30m));
        ReferenceEquals(original, reajustado).ShouldBeFalse();
    }

    [Fact]
    public void Produto_Desativar_RetornaCopiaInativaEIgualdadePorValor()
    {
        var produto = NovoProduto();
        var inativo = produto.Desativar();

        produto.Ativo.ShouldBeTrue();
        inativo.Ativo.ShouldBeFalse();
        inativo.ShouldNotBe(produto);
        (inativo with { Ativo = true }).ShouldBe(produto);
    }

    [Fact]
    public void Produto_NomeEmBranco_LancaInclusiveViaWith()
    {
        Should.Throw<ArgumentException>(() => NovoProduto(nome: "  "));
        var valido = NovoProduto(nome: "  Pizza  ");
        valido.Nome.ShouldBe("Pizza");
        Should.Throw<ArgumentException>(() => valido with { Nome = "" });
    }

    [Fact]
    public void Produto_Resumo_UsaDescricaoApenasQuandoPreenchida()
    {
        NovoProduto().Resumo().ShouldBe("X-Burger");
        (NovoProduto() with { Descricao = "" }).Resumo().ShouldBe("X-Burger");
        (NovoProduto() with { Descricao = "Pão, carne e queijo" }).Resumo().ShouldBe("X-Burger — Pão, carne e queijo");
    }
}
