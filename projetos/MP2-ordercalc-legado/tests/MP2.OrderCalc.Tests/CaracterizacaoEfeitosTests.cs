namespace MP2.OrderCalc.Tests;

/// <summary>
/// Caracterização de efeitos que só aparecem em SEQUÊNCIA de chamadas
/// (o golden master zera o Db a cada cenário).
/// </summary>
public class CaracterizacaoEfeitosTests
{
    public CaracterizacaoEfeitosTests() => Db.Reset();

    [Fact]
    public void Calcular_DoisPedidos_NumeroUsaDataHoraESequencial()
    {
        var calc = new OrderCalculator(Cenarios.Relogio());

        var p1 = calc.Calcular("C1", "MS-002:1", null!, "SP", "NORMAL");
        var p2 = calc.Calcular("C2", "MS-002:1", null!, "SP", "NORMAL");

        p1.Numero.ShouldBe("PED-20260310103000-0001");
        p2.Numero.ShouldBe("PED-20260310103000-0002");
        Db.Pedidos.Select(p => p.Numero).ShouldBe([p1.Numero, p2.Numero]);
    }

    [Fact]
    public void Calcular_PedidoComErro_NaoConsomeNumeroNemGrava()
    {
        var calc = new OrderCalculator(Cenarios.Relogio());

        calc.Calcular("C1", "LV-005:99", null!, "SP", "NORMAL").Status.ShouldBe("ERRO");
        var ok = calc.Calcular("C1", "LV-005:1", null!, "SP", "NORMAL");

        ok.Numero.ShouldEndWith("-0001");
        Db.Pedidos.Count.ShouldBe(1);
        Db.Produtos["LV-005"].Estoque.ShouldBe(4);
    }

    [Fact]
    public void Calcular_ClienteNovo_SoGanhaBoasVindasNoPrimeiroPedido()
    {
        var calc = new OrderCalculator(Cenarios.Relogio());

        var primeiro = calc.Calcular("C3", "LV-004:1", null!, "SP", "NORMAL");
        var segundo = calc.Calcular("C3", "LV-004:1", null!, "SP", "NORMAL");

        primeiro.Mensagens.ShouldContain("Desconto de boas-vindas");
        segundo.Mensagens.ShouldNotContain("Desconto de boas-vindas");
        Db.Clientes["C3"].Tipo.ShouldBe("NORMAL");
    }

    [Fact]
    public void Calcular_VipQueMantemDesconto_AindaAssimConsomeOCupom()
    {
        // Quirk do legado: o cupom não foi usado no cálculo, mas o uso é debitado.
        var calc = new OrderCalculator(Cenarios.Relogio(Cenarios.BlackFriday));

        var r = calc.Calcular("C2", "TC-003:1", "BEMVINDO10", "SP", "NORMAL");

        r.Mensagens.ShouldContain("Desconto VIP mantido (cupom não acumula)");
        Db.Cupons["BEMVINDO10"].UsosRestantes.ShouldBe(999);
    }

    [Fact]
    public void Calcular_EstoqueAcabaEntrePedidos_SegundoPedidoFalha()
    {
        var calc = new OrderCalculator(Cenarios.Relogio());

        calc.Calcular("C1", "CD-006:3", null!, "SP", "NORMAL").Status.ShouldBe("OK");
        var r = calc.Calcular("C1", "CD-006:1", null!, "SP", "NORMAL");

        r.Status.ShouldBe("ERRO");
        r.Erro.ShouldBe("Estoque insuficiente: CD-006");
    }

    [Fact]
    public void CalcularOrcamento_NuncaAlteraOBanco()
    {
        var calc = new OrderCalculator(Cenarios.Relogio());

        calc.CalcularOrcamento("C3", "LV-004:1;CD-006:3", "SP", "EXPRESSO").Status.ShouldBe("ORCAMENTO");

        Db.Pedidos.ShouldBeEmpty();
        Db.ProximoId.ShouldBe(1);
        Db.Produtos["CD-006"].Estoque.ShouldBe(3);
        Db.Clientes["C3"].Tipo.ShouldBe("NOVO");
    }

    [Fact]
    public void Calcular_CriadoEmEPrazo_VemDoRelogio()
    {
        var calc = new OrderCalculator(Cenarios.Relogio());

        var r = calc.Calcular("C1", "MS-002:1", null!, "BA", "NORMAL");

        r.CriadoEm.ShouldBe(new DateTime(2026, 3, 10, 10, 30, 0));
        r.PrazoEntrega.ShouldBe(new DateTime(2026, 3, 19)); // 7 dias úteis a partir de terça 10/03
    }
}
