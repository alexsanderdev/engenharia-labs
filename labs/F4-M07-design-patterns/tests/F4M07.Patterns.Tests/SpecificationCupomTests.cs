using F4M07.Patterns.Cupons;

namespace F4M07.Patterns.Tests;

/// <summary>Passo 5 — SPECIFICATION: regras de elegibilidade de cupom pequenas, nomeadas e combináveis.</summary>
public sealed class SpecificationCupomTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 27, 12, 0, 0, TimeSpan.FromHours(-3));

    private static ContextoDoCupom Contexto(
        decimal subtotal = 150m,
        int pedidosAnteriores = 0,
        CategoriaDoCliente categoria = CategoriaDoCliente.Comum,
        DateTimeOffset? agora = null,
        params string[] categorias) =>
        new(subtotal, pedidosAnteriores, categoria, categorias.Length == 0 ? ["Cafés"] : categorias, agora ?? Agora);

    [Fact]
    public void RegrasSimples_AvaliamUmaCoisaSo()
    {
        new SubtotalMinimo(100m).EhSatisfeitaPor(Contexto(subtotal: 100m)).ShouldBeTrue();
        new SubtotalMinimo(100m).EhSatisfeitaPor(Contexto(subtotal: 99.99m)).ShouldBeFalse();
        new PrimeiraCompra().EhSatisfeitaPor(Contexto(pedidosAnteriores: 0)).ShouldBeTrue();
        new PrimeiraCompra().EhSatisfeitaPor(Contexto(pedidosAnteriores: 3)).ShouldBeFalse();
        new ClienteVip().EhSatisfeitaPor(Contexto(categoria: CategoriaDoCliente.Vip)).ShouldBeTrue();
        new ContemCategoria("promoções").EhSatisfeitaPor(Contexto(categorias: ["Cafés", "Promoções"])).ShouldBeTrue();
        new ContemCategoria("Promoções").EhSatisfeitaPor(Contexto(categorias: ["Cafés"])).ShouldBeFalse();
    }

    [Fact]
    public void Vigente_InicioInclusivoFimExclusivo()
    {
        var inicio = new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.FromHours(-3));
        var fim = inicio.AddDays(3);
        var regra = new Vigente(inicio, fim);

        regra.EhSatisfeitaPor(Contexto(agora: inicio)).ShouldBeTrue();
        regra.EhSatisfeitaPor(Contexto(agora: fim.AddTicks(-1))).ShouldBeTrue();
        regra.EhSatisfeitaPor(Contexto(agora: fim)).ShouldBeFalse();
        regra.EhSatisfeitaPor(Contexto(agora: inicio.AddTicks(-1))).ShouldBeFalse();
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void E_SoEhSatisfeitaComAsDuas(bool a, bool b, bool esperado) =>
        (Fixa(a) & Fixa(b)).EhSatisfeitaPor(Contexto()).ShouldBe(esperado);

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void Ou_BastaUma(bool a, bool b, bool esperado) =>
        Fixa(a).Ou(Fixa(b)).EhSatisfeitaPor(Contexto()).ShouldBe(esperado);

    [Fact]
    public void Nao_InverteERespeitaCurtoCircuito()
    {
        (!Fixa(true)).EhSatisfeitaPor(Contexto()).ShouldBeFalse();
        Fixa(false).Nao().EhSatisfeitaPor(Contexto()).ShouldBeTrue();

        var explode = new Explode();
        (Fixa(false) & explode).EhSatisfeitaPor(Contexto()).ShouldBeFalse();
        (Fixa(true) | explode).EhSatisfeitaPor(Contexto()).ShouldBeTrue();
    }

    [Fact]
    public void Descricao_DaRegraComposta_EhLegivelParaSuporte()
    {
        RegrasDeCupom.BemVindo10().Descricao
            .ShouldBe("((primeira compra E subtotal >= 100,00) E NÃO contém categoria Promoções)");
    }

    [Theory]
    [InlineData(150, 0, false, true)]   // primeira compra, R$ 150, sem promoção
    [InlineData(150, 1, false, false)]  // não é a primeira compra
    [InlineData(99, 0, false, false)]   // abaixo do mínimo
    [InlineData(150, 0, true, false)]   // carrinho tem item de Promoções
    public void BemVindo10_PrimeiraCompraAcimaDe100SemPromocoes(decimal subtotal, int anteriores, bool temPromocao, bool elegivel)
    {
        string[] categorias = temPromocao ? ["Cafés", "Promoções"] : ["Cafés"];

        RegrasDeCupom.BemVindo10().EhSatisfeitaPor(Contexto(subtotal, anteriores, categorias: categorias)).ShouldBe(elegivel);
    }

    [Fact]
    public void FreteVip_VipOuSubtotalAlto()
    {
        var regra = RegrasDeCupom.FreteVip();

        regra.EhSatisfeitaPor(Contexto(subtotal: 50m, categoria: CategoriaDoCliente.Vip)).ShouldBeTrue();
        regra.EhSatisfeitaPor(Contexto(subtotal: 500m)).ShouldBeTrue();
        regra.EhSatisfeitaPor(Contexto(subtotal: 499.99m)).ShouldBeFalse();
    }

    [Fact]
    public void BlackFriday_VigenteSubtotal200EVipOuClienteRecorrente()
    {
        var inicio = new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.FromHours(-3));
        var regra = RegrasDeCupom.BlackFriday(inicio, inicio.AddDays(3));

        regra.EhSatisfeitaPor(Contexto(250m, pedidosAnteriores: 2)).ShouldBeTrue();
        regra.EhSatisfeitaPor(Contexto(250m, pedidosAnteriores: 0, CategoriaDoCliente.Vip)).ShouldBeTrue();
        regra.EhSatisfeitaPor(Contexto(250m, pedidosAnteriores: 0)).ShouldBeFalse("primeira compra de cliente comum não entra");
        regra.EhSatisfeitaPor(Contexto(199m, pedidosAnteriores: 2)).ShouldBeFalse();
        regra.EhSatisfeitaPor(Contexto(250m, pedidosAnteriores: 2, agora: inicio.AddDays(3))).ShouldBeFalse("fora da vigência");
    }

    private static Especificacao<ContextoDoCupom> Fixa(bool valor) => new Constante(valor);

    private sealed class Constante(bool valor) : Especificacao<ContextoDoCupom>
    {
        public override bool EhSatisfeitaPor(ContextoDoCupom candidato) => valor;

        public override string Descricao => valor ? "sempre" : "nunca";
    }

    private sealed class Explode : Especificacao<ContextoDoCupom>
    {
        public override bool EhSatisfeitaPor(ContextoDoCupom candidato) =>
            throw new InvalidOperationException("Curto-circuito: esta regra não deveria ser avaliada.");

        public override string Descricao => "explode";
    }
}
