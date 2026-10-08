using F2M01.CleanCode.Legado;

namespace F2M01.CleanCode.Tests;

/// <summary>
/// Testes de DESIGN: exercitam a API limpa que você vai extrair do legado.
/// Começam vermelhos; ficam verdes quando a lógica migra para as classes novas.
/// </summary>
public class DesignValidadorDePedidoTests
{
    private readonly ValidadorDePedido _validador = new();

    [Fact]
    public void Validar_PedidoValido_NaoTemErros()
    {
        var resultado = _validador.Validar(new PedidoParaCalculo([new ItemDoPedido("A", 10m, 1)], "SP"));

        resultado.EhValido.ShouldBeTrue();
        resultado.Erros.ShouldBeEmpty();
    }

    [Fact]
    public void Validar_SemItens_ApontaApenasPedidoSemItens()
    {
        var resultado = _validador.Validar(new PedidoParaCalculo([], "S"));

        resultado.Erros.ShouldBe(["Pedido sem itens"]);
    }

    [Fact]
    public void Validar_VariosProblemas_ListaTodosNaOrdemEncontrada()
    {
        var pedido = new PedidoParaCalculo(
            [new ItemDoPedido("A", 10m, 0), new ItemDoPedido("B", 10m, 1, ProdutoAtivo: false), new ItemDoPedido("C", 10m, -1, ProdutoAtivo: false)],
            "X");

        _validador.Validar(pedido).Erros.ShouldBe(
            ["Quantidade inválida: A", "Produto inativo: B", "Quantidade inválida: C", "UF inválida"]);
    }
}

public class DesignRegrasDeFreteTests
{
    private readonly RegrasDeFrete _frete = new();

    [Theory]
    [InlineData("SP", 15)]
    [InlineData("RJ", 25)]
    [InlineData("MG", 25)]
    [InlineData("ES", 25)]
    [InlineData("PR", 25)]
    [InlineData("SC", 25)]
    [InlineData("RS", 25)]
    [InlineData("BA", 40)]
    [InlineData("AM", 40)]
    public void Calcular_EntregaPadraoAbaixoDoMinimo_CobraPorRegiao(string uf, int esperado)
    {
        _frete.Calcular(uf, 100m, ModalidadeDeEntrega.Padrao).ShouldBe(esperado);
    }

    [Fact]
    public void Calcular_ValorNoMinimoParaFreteGratis_NaoCobra()
    {
        _frete.Calcular("BA", RegrasDeFrete.ValorMinimoParaFreteGratis, ModalidadeDeEntrega.Padrao).ShouldBe(0m);
        _frete.Calcular("BA", RegrasDeFrete.ValorMinimoParaFreteGratis - 0.01m, ModalidadeDeEntrega.Padrao).ShouldBe(40m);
    }

    [Fact]
    public void Calcular_EntregaExpressa_DobraMesmoAcimaDoMinimo()
    {
        _frete.Calcular("SP", 1000m, ModalidadeDeEntrega.Expressa).ShouldBe(30m);
    }

    [Fact]
    public void Calcular_UfMinusculaComEspacos_EhNormalizada()
    {
        _frete.Calcular(" rj ", 100m, ModalidadeDeEntrega.Padrao).ShouldBe(25m);
    }
}

public class DesignCalculadoraDePedidoTests
{
    private readonly CalculadoraDePedido _calculadora = new();

    [Fact]
    public void Calcular_ClienteComum_DevolveResumoDetalhado()
    {
        var resumo = _calculadora.Calcular(new PedidoParaCalculo([new ItemDoPedido("A", 50m, 2)], "SP"));

        resumo.ShouldBe(new ResumoDoPedido(Subtotal: 100m, Desconto: 0m, Frete: 15m));
        resumo.Total.ShouldBe(115m);
    }

    [Fact]
    public void Calcular_ClienteComumAcimaDoVolume_Ganha5PorCentoEFreteGratis()
    {
        var resumo = _calculadora.Calcular(new PedidoParaCalculo([new ItemDoPedido("A", 300m, 2)], "RJ"));

        resumo.ShouldBe(new ResumoDoPedido(Subtotal: 600m, Desconto: 30m, Frete: 0m));
    }

    [Fact]
    public void Calcular_ClienteVip_Ganha10PorCentoSemAcumular()
    {
        var resumo = _calculadora.Calcular(new PedidoParaCalculo([new ItemDoPedido("A", 1000m, 1)], "BA", TipoDeCliente.Vip));

        resumo.Desconto.ShouldBe(100m);
        resumo.Total.ShouldBe(900m);
    }

    [Fact]
    public void Calcular_EntregaExpressa_UsaAsRegrasDeFrete()
    {
        var resumo = _calculadora.Calcular(new PedidoParaCalculo(
            [new ItemDoPedido("A", 400m, 1)], "SP", TipoDeCliente.Comum, ModalidadeDeEntrega.Expressa));

        resumo.Frete.ShouldBe(30m);
        resumo.Total.ShouldBe(430m);
    }

    [Fact]
    public void Calcular_PedidoInvalido_LancaExcecaoComTodosOsErros()
    {
        var pedido = new PedidoParaCalculo([new ItemDoPedido("A", 10m, 0)], "X");

        var ex = Should.Throw<PedidoInvalidoException>(() => _calculadora.Calcular(pedido));

        ex.Erros.ShouldBe(["Quantidade inválida: A", "UF inválida"]);
    }
}

/// <summary>
/// A prova final da refatoração: a fachada legada e a API nova dão o MESMO resultado.
/// (Na solução, a fachada delega para a API nova — o método gigante desaparece.)
/// </summary>
public class DesignEquivalenciaTests
{
    [Theory]
    [InlineData("50.00", 2, "SP", false, false)]
    [InlineData("600.00", 1, "RJ", false, false)]
    [InlineData("100.00", 2, "MG", true, false)]
    [InlineData("1000.00", 1, "BA", true, false)]
    [InlineData("299.99", 1, "AM", false, false)]
    [InlineData("400.00", 1, "sp", false, true)]
    [InlineData("33.35", 3, "BA", true, true)]
    public void FachadaLegadaEApiNova_ConcordamNoTotal(string preco, int quantidade, string uf, bool vip, bool expressa)
    {
        List<ItemDoPedido> itens = [new ItemDoPedido("A", decimal.Parse(preco, System.Globalization.CultureInfo.InvariantCulture), quantidade)];

        var totalLegado = PedidoUtil.Calc(itens, uf, vip, expressa, out _);
        var resumoNovo = new CalculadoraDePedido().Calcular(new PedidoParaCalculo(
            itens,
            uf,
            vip ? TipoDeCliente.Vip : TipoDeCliente.Comum,
            expressa ? ModalidadeDeEntrega.Expressa : ModalidadeDeEntrega.Padrao));

        resumoNovo.Total.ShouldBe(totalLegado);
    }

    [Fact]
    public void FachadaLegadaEApiNova_ConcordamNoPrimeiroErro()
    {
        List<ItemDoPedido> itens = [new ItemDoPedido("A", 10m, 1), new ItemDoPedido("B", 10m, 1, ProdutoAtivo: false)];

        PedidoUtil.Calc(itens, "XX", false, false, out var mensagemLegada);
        var ex = Should.Throw<PedidoInvalidoException>(() => new CalculadoraDePedido().Calcular(new PedidoParaCalculo(itens, "XX")));

        ex.Erros[0].ShouldBe(mensagemLegada);
    }
}
