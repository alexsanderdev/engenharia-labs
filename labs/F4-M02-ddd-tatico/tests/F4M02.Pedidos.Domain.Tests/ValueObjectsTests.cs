namespace F4M02.Pedidos.Domain.Tests;

/// <summary>
/// Passo 1 — Value objects: sem identidade, imutáveis, autovalidados, iguais quando os valores são iguais.
/// </summary>
public sealed class ValueObjectsTests
{
    [Fact]
    public void Dinheiro_IgualdadePorValor_EOperacoesDevolvemNovoValorSemMisturarMoedas()
    {
        var a = new Dinheiro(10.0m, "brl");
        var b = new Dinheiro(10.00m, " BRL ");

        a.Moeda.ShouldBe("BRL");
        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
        a.ShouldNotBe(new Dinheiro(10m, "USD"));
        a.ToString().ShouldBe("BRL 10.00");

        var dez = Dinheiro.Reais(10m);
        var doisEMeio = Dinheiro.Reais(2.5m);

        (dez + doisEMeio).ShouldBe(Dinheiro.Reais(12.5m));
        (dez - doisEMeio).ShouldBe(Dinheiro.Reais(7.5m));
        (dez * new Quantidade(3)).ShouldBe(Dinheiro.Reais(30m));
        Dinheiro.Reais(33.33m).Percentual(5).ShouldBe(Dinheiro.Reais(1.67m)); // 1,6665 → 1,67
        dez.ShouldBe(Dinheiro.Reais(10m)); // o original não mudou

        Dado.RegraVioladaPor(() => _ = dez + new Dinheiro(1m, "USD")).ShouldBe(Regras.MoedasDiferentes);
        Dado.RegraVioladaPor(() => _ = doisEMeio - dez).ShouldBe(Regras.DinheiroNegativo);
    }

    [Theory]
    [InlineData(-0.01, "BRL", Regras.DinheiroNegativo)]
    [InlineData(10, "", Regras.MoedaInvalida)]
    [InlineData(10, "R$", Regras.MoedaInvalida)]
    [InlineData(10, "REAL", Regras.MoedaInvalida)]
    [InlineData(10, "B1L", Regras.MoedaInvalida)]
    public void Dinheiro_ValorNegativoOuMoedaInvalida_EhRejeitado(decimal valor, string moeda, string regra)
    {
        Dado.RegraVioladaPor(() => _ = new Dinheiro(valor, moeda)).ShouldBe(regra);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(999, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1000, false)]
    public void Quantidade_SoExisteEntreUmEOMaximo(int valor, bool valida)
    {
        if (valida)
        {
            new Quantidade(valor).ShouldBe(new Quantidade(valor));
            new Quantidade(valor).Valor.ShouldBe(valor);
        }
        else
        {
            Dado.RegraVioladaPor(() => _ = new Quantidade(valor)).ShouldBe(Regras.QuantidadeInvalida);
        }
    }

    [Theory]
    [InlineData(" cafe-500g ", "CAFE-500G")]
    [InlineData("ABC", "ABC")]
    [InlineData("x1-y2-z3", "X1-Y2-Z3")]
    [InlineData("", null)]
    [InlineData("AB", null)]
    [InlineData("CAFE 500G", null)]
    [InlineData("CAFE--500G", null)]
    [InlineData("-CAFE", null)]
    [InlineData("CAFE-500G-TORRADO-EXTRA", null)] // 23 caracteres
    public void Sku_EhNormalizado_EForaDoPadraoEhRejeitado(string entrada, string? esperado)
    {
        if (esperado is not null)
            new Sku(entrada).Valor.ShouldBe(esperado);
        else
            Dado.RegraVioladaPor(() => _ = new Sku(entrada)).ShouldBe(Regras.SkuInvalido);
    }

    [Theory]
    [InlineData(" Av. Paulista ", "1000", "São Paulo", "sp", "01310-100", true)]
    [InlineData("Av. Paulista", "1000", "São Paulo", "SP", "01310100", true)]
    [InlineData("", "1000", "São Paulo", "SP", "01310100", false)]
    [InlineData("Av. Paulista", " ", "São Paulo", "SP", "01310100", false)]
    [InlineData("Av. Paulista", "1000", "", "SP", "01310100", false)]
    [InlineData("Av. Paulista", "1000", "São Paulo", "S", "01310100", false)]
    [InlineData("Av. Paulista", "1000", "São Paulo", "S1", "01310100", false)]
    [InlineData("Av. Paulista", "1000", "São Paulo", "SP", "0131010", false)]
    [InlineData("Av. Paulista", "1000", "São Paulo", "SP", "0131O-100", false)]
    public void EnderecoDeEntrega_EhNormalizado_ComparadoPorValor_EInvalidoEhRejeitado(
        string logradouro, string numero, string cidade, string uf, string cep, bool valido)
    {
        if (!valido)
        {
            Dado.RegraVioladaPor(() => _ = new EnderecoDeEntrega(logradouro, numero, cidade, uf, cep))
                .ShouldBe(Regras.EnderecoInvalido);
            return;
        }

        var endereco = new EnderecoDeEntrega(logradouro, numero, cidade, uf, cep);

        endereco.Logradouro.ShouldBe("Av. Paulista");
        endereco.Uf.ShouldBe("SP");
        endereco.Cep.ShouldBe("01310100");
        endereco.ShouldBe(new EnderecoDeEntrega("Av. Paulista", "1000", "São Paulo", "SP", "01310100"));
        endereco.ShouldNotBe(new EnderecoDeEntrega("Av. Paulista", "1001", "São Paulo", "SP", "01310100"));
    }

    [Fact]
    public void PedidoId_Novo_EhUnicoENuncaVazio_EIdVazioEhRejeitado()
    {
        var a = PedidoId.Novo();
        var b = PedidoId.Novo();

        a.ShouldNotBe(b);
        a.Valor.ShouldNotBe(Guid.Empty);
        new PedidoId(a.Valor).ShouldBe(a); // igualdade por valor
        a.ToString().ShouldBe(a.Valor.ToString());
        Should.Throw<ArgumentException>(() => _ = new PedidoId(Guid.Empty));
    }
}
