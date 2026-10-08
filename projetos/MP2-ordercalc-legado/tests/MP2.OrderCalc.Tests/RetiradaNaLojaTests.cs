namespace MP2.OrderCalc.Tests;

/// <summary>
/// Regra NOVA, adicionada com TDD depois da refatoração:
/// "RETIRADA" na loja (só SP): frete zero, prazo de 1 dia útil, imposto normal.
/// Repare como cada teste coube em poucas linhas e mexeu em um lugar só do código.
/// </summary>
public class RetiradaNaLojaTests
{
    public RetiradaNaLojaTests() => Db.Reset();

    private static OrderCalculator Calc() => new(Cenarios.Relogio());

    [Fact]
    public void Calcular_RetiradaEmSp_FreteZeroEMensagem()
    {
        var r = Calc().Calcular("C1", "MS-002:1", null!, "SP", "RETIRADA");

        r.Status.ShouldBe("OK");
        r.Frete.ShouldBe(0m);
        r.Mensagens.ShouldBe(["Retirada na loja"]);
    }

    [Fact]
    public void Calcular_RetiradaEmSp_PrazoDeUmDiaUtil()
    {
        var sexta = new DateTimeOffset(2026, 3, 13, 18, 0, 0, TimeSpan.Zero);

        var r = new OrderCalculator(Cenarios.Relogio(sexta)).Calcular("C1", "MS-002:1", null!, "SP", "RETIRADA");

        r.PrazoEntrega.ShouldBe(new DateTime(2026, 3, 16)); // segunda-feira
    }

    [Fact]
    public void Calcular_RetiradaEmSp_ImpostoETotalComoNoFreteNormal()
    {
        var r = Calc().Calcular("C1", "MS-002:1", null!, "SP", "RETIRADA");

        r.Imposto.ShouldBe(16.18m);
        r.Total.ShouldBe(106.08m); // 89,90 + 16,18, sem os R$ 15 de frete
    }

    [Fact]
    public void Calcular_RetiradaForaDeSp_Erro()
    {
        var r = Calc().Calcular("C1", "MS-002:1", null!, "RJ", "RETIRADA");

        r.Status.ShouldBe("ERRO");
        r.Erro.ShouldBe("Retirada disponível apenas em SP");
        Db.Pedidos.ShouldBeEmpty();
    }

    [Fact]
    public void Calcular_RetiradaComCupomDeFrete_NaoDuplicaMensagemDeFrete()
    {
        var r = Calc().Calcular("C1", "TC-003:1", "FRETEGRATIS", "SP", "RETIRADA");

        r.Frete.ShouldBe(0m);
        r.Mensagens.ShouldBe(["Retirada na loja"]);
    }

    [Fact]
    public void CalcularOrcamento_Retirada_TambemFunciona()
    {
        var r = Calc().CalcularOrcamento("C2", "NB-001:1", "sp", "RETIRADA");

        r.Status.ShouldBe("ORCAMENTO");
        r.Frete.ShouldBe(0m);
    }
}
