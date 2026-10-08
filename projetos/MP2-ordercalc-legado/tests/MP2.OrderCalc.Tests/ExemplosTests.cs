namespace MP2.OrderCalc.Tests;

// Dois exemplos para você começar. A suíte de caracterização completa é SUA missão (ver README).
public class ExemplosTests
{
    public ExemplosTests() => Db.Reset();

    [Fact]
    public void CalcularOrcamento_ClienteNormalEmSp_SomaItensImpostoEFreteGratis()
    {
        var calc = new OrderCalculator();

        var r = calc.CalcularOrcamento("C1", "MS-002:2;LV-004:1", "SP", "NORMAL");

        r.Status.ShouldBe("ORCAMENTO");
        r.Subtotal.ShouldBe(368.80m);
        r.Frete.ShouldBe(0m);       // subtotal >= 300 no frete NORMAL
        r.Imposto.ShouldBe(32.36m); // 18% só sobre o mouse: livro é isento
        r.Total.ShouldBe(401.16m);
    }

    [Fact]
    public void Calcular_ClienteNormalComCupomNoRj_ExemploDeGoldenMaster()
    {
        var calc = new OrderCalculator();

        var r = calc.Calcular("C1", "NB-001:1;CB-008:2", "VALE20", "RJ", "EXPRESSO");

        // Numero, CriadoEm e PrazoEntrega ficaram de fora porque dependem de DateTime.Now.
        // Sua primeira missão é criar um seam para controlar o tempo e poder incluí-los.
        Aprovacao.Verificar(new { r.Status, r.Erro, r.Subtotal, r.Desconto, r.Frete, r.Imposto, r.Total, r.Linhas, r.Mensagens });
    }
}
