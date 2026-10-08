using F2M03.CodeSmells.SwitchStatements;

namespace F2M03.CodeSmells.Tests;

/// <summary>Caracterização do processador com switches repetidos. Nunca podem ficar vermelhos.</summary>
public class ComportamentoSwitchStatementsTests
{
    [Theory]
    [InlineData(TipoDePagamento.Pix, 100, 0, 0, 0, "Pix")]
    [InlineData(TipoDePagamento.Boleto, 100, 1, 3.50, 2, "Boleto bancário")]
    [InlineData(TipoDePagamento.CartaoDeCredito, 100, 1, 2.99, 30, "Cartão de crédito em 1x")]
    [InlineData(TipoDePagamento.CartaoDeCredito, 200, 3, 7.98, 30, "Cartão de crédito em 3x")]
    [InlineData(TipoDePagamento.CartaoDeCredito, 33.33, 12, 2.83, 30, "Cartão de crédito em 12x")]
    public void Processador_PorTipo_TaxaPrazoEDescricao(TipoDePagamento tipo, decimal valor, int parcelas, decimal taxa, int dias, string descricao)
    {
        var processador = new ProcessadorDePagamentos();

        processador.CalcularTaxa(tipo, valor, parcelas).ShouldBe(taxa);
        processador.DiasParaCompensar(tipo).ShouldBe(dias);
        processador.Descrever(tipo, parcelas).ShouldBe(descricao);
    }

    [Fact]
    public void Processador_ParcelasInvalidasOuTipoDesconhecido_Lanca()
    {
        var processador = new ProcessadorDePagamentos();

        Should.Throw<ArgumentOutOfRangeException>(() => processador.CalcularTaxa(TipoDePagamento.CartaoDeCredito, 100m, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => processador.Descrever(TipoDePagamento.CartaoDeCredito, 13));
        Should.Throw<ArgumentOutOfRangeException>(() => processador.DiasParaCompensar((TipoDePagamento)99));
    }
}
