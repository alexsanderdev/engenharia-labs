using F2M03.CodeSmells.SwitchStatements;

namespace F2M03.CodeSmells.Tests;

/// <summary>Design: polimorfismo no lugar dos switches. Começam vermelhos.</summary>
public class DesignSwitchStatementsTests
{
    [Fact]
    public void Criar_DevolveOMeioCertoECadaUmSabeSuasRegras()
    {
        var pix = MeioDePagamento.Criar(TipoDePagamento.Pix).ShouldBeOfType<Pix>();
        var boleto = MeioDePagamento.Criar(TipoDePagamento.Boleto).ShouldBeOfType<Boleto>();
        var cartao = MeioDePagamento.Criar(TipoDePagamento.CartaoDeCredito, 3).ShouldBeOfType<CartaoDeCredito>();

        (pix.CalcularTaxa(100m), pix.DiasParaCompensar, pix.Descricao).ShouldBe((0m, 0, "Pix"));
        (boleto.CalcularTaxa(100m), boleto.DiasParaCompensar, boleto.Descricao).ShouldBe((3.50m, 2, "Boleto bancário"));
        (cartao.CalcularTaxa(200m), cartao.DiasParaCompensar, cartao.Descricao).ShouldBe((7.98m, 30, "Cartão de crédito em 3x"));
        Should.Throw<ArgumentOutOfRangeException>(() => new CartaoDeCredito(13));
    }

    /// <summary>Um meio que NÃO existe no código de produção: o processador funciona sem ser alterado.</summary>
    private sealed class Cashback : MeioDePagamento
    {
        public override decimal CalcularTaxa(decimal valor) => valor * 0.01m;
        public override int DiasParaCompensar => 7;
        public override string Descricao => "Cashback";
    }

    [Fact]
    public void Resumir_MeioNovo_FuncionaSemMudarOProcessador()
    {
        ProcessadorDePagamentos.Resumir(new Cashback(), 250m).ShouldBe("Cashback: taxa R$ 2.50, compensa em 7 dia(s)");
        ProcessadorDePagamentos.Resumir(MeioDePagamento.Criar(TipoDePagamento.Boleto), 10m).ShouldBe("Boleto bancário: taxa R$ 3.50, compensa em 2 dia(s)");
    }
}
