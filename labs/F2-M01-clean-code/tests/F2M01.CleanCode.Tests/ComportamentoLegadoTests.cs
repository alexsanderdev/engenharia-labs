using F2M01.CleanCode.Legado;

namespace F2M01.CleanCode.Tests;

/// <summary>
/// Testes de CARACTERIZAÇÃO: registram o que o legado FAZ hoje (inclusive onde os comentários mentem).
/// Passam desde o início e precisam continuar verdes em TODOS os passos da refatoração.
/// </summary>
public class ComportamentoLegadoTests
{
    private static ItemDoPedido Item(string sku, decimal preco, int quantidade, bool ativo = true) =>
        new(sku, preco, quantidade, ativo);

    private static decimal Dinheiro(string valor) =>
        decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void Calc_ClienteComumEmSp_SemDesconto_CobraFreteDe15()
    {
        var total = PedidoUtil.Calc([Item("A", 50m, 2)], "SP", false, false, out var mensagem);

        total.ShouldBe(115m);
        mensagem.ShouldBeNull();
    }

    [Fact]
    public void Calc_ClienteComumAcimaDe500_Ganha5PorCentoEFreteGratis()
    {
        PedidoUtil.Calc([Item("A", 600m, 1)], "RJ", false, false, out _).ShouldBe(570m);
    }

    [Fact]
    public void Calc_ClienteVip_Ganha10PorCento_NaoOs15DoComentario()
    {
        // 200 - 10% = 180 (< 300) + frete MG 25
        PedidoUtil.Calc([Item("A", 100m, 2)], "MG", true, false, out _).ShouldBe(205m);
    }

    [Fact]
    public void Calc_VipAcimaDe500_DescontosNaoAcumulam()
    {
        PedidoUtil.Calc([Item("A", 1000m, 1)], "BA", true, false, out _).ShouldBe(900m);
    }

    [Theory]
    [InlineData("250.00", "290.00")]   // abaixo de 300: paga frete de BA (40), apesar do comentário "grátis acima de 200"
    [InlineData("299.99", "339.99")]
    [InlineData("300.00", "300.00")]   // a partir de 300: frete grátis
    public void Calc_FreteGratisComecaEm300_NaoEm200DoComentario(string preco, string esperado)
    {
        PedidoUtil.Calc([Item("A", Dinheiro(preco), 1)], "BA", false, false, out _).ShouldBe(Dinheiro(esperado));
    }

    [Fact]
    public void Calc_EntregaExpressa_DobraOFreteEIgnoraFreteGratis()
    {
        PedidoUtil.Calc([Item("A", 400m, 1)], "SP", false, true, out _).ShouldBe(430m);
    }

    [Fact]
    public void Calc_UfMinusculaComEspacos_EhAceita()
    {
        PedidoUtil.Calc([Item("A", 100m, 1)], " sp ", false, false, out _).ShouldBe(115m);
    }

    [Fact]
    public void Calc_DescontoEhArredondadoParaDuasCasas()
    {
        // subtotal 100,05 → 10% = 10,005 → 10,01 (AwayFromZero) → 90,04 + frete BA 40
        PedidoUtil.Calc([Item("A", 33.35m, 3)], "BA", true, false, out _).ShouldBe(130.04m);
    }

    public static TheoryData<string, List<ItemDoPedido>, string, string> PedidosInvalidos => new()
    {
        { "sem itens", [], "SP", "Pedido sem itens" },
        { "quantidade zero", [Item("A", 10m, 0)], "SP", "Quantidade inválida: A" },
        { "produto inativo", [Item("B", 10m, 1, ativo: false)], "SP", "Produto inativo: B" },
        { "uf curta", [Item("A", 10m, 1)], "S", "UF inválida" },
        { "uf vazia", [Item("A", 10m, 1)], "", "UF inválida" },
    };

    [Theory]
    [MemberData(nameof(PedidosInvalidos))]
    public void Calc_PedidoInvalido_RetornaMenosUmEAMensagem(string cenario, List<ItemDoPedido> itens, string uf, string mensagemEsperada)
    {
        _ = cenario;
        var total = PedidoUtil.Calc(itens, uf, false, false, out var mensagem);

        total.ShouldBe(-1m);
        mensagem.ShouldBe(mensagemEsperada);
    }

    [Fact]
    public void Calc_VariosProblemas_RetornaSoOPrimeiro()
    {
        var total = PedidoUtil.Calc([Item("A", 10m, 0), Item("B", 10m, 1, ativo: false)], "X", false, false, out var mensagem);

        total.ShouldBe(-1m);
        mensagem.ShouldBe("Quantidade inválida: A");
    }

    [Fact]
    public void Calc_ListaNula_TrataComoPedidoSemItens()
    {
        PedidoUtil.Calc(null!, "SP", false, false, out var mensagem).ShouldBe(-1m);
        mensagem.ShouldBe("Pedido sem itens");
    }
}
