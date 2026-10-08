namespace F1M04.Linq.Tests;

public class ConsultasPedidosTests
{
    [Fact]
    public void Total_SomaOsSubtotaisDosItens()
    {
        var pedido = Dados.Pedidos().First(p => p.Id == 100);

        ConsultasPedidos.Total(pedido).ShouldBe(300m);
    }

    [Fact]
    public void FaturamentoPorCliente_IgnoraCancelados()
    {
        var faturamento = ConsultasPedidos.FaturamentoPorCliente(Dados.Pedidos());

        faturamento.Count.ShouldBe(2);
        faturamento[10].ShouldBe(550m);
        faturamento[20].ShouldBe(1500m); // o pedido cancelado de 3.500 não entra
    }

    [Fact]
    public void ResumoPorCliente_JuntaAgrupaEOrdenaPeloTotalGasto()
    {
        var resumo = ConsultasPedidos.ResumoPorCliente(Dados.Pedidos(), Dados.Clientes());

        resumo.ShouldBe(
        [
            new ResumoCliente("Bruno", 1, 1500m),
            new ResumoCliente("Ana", 3, 550m),
        ]);
    }

    [Fact]
    public void MaisVendidos_Top2_DevolveOsDeMaiorQuantidade()
    {
        var top = ConsultasPedidos.MaisVendidos(Dados.Pedidos(), Dados.Produtos(), 2);

        top.ShouldBe([new ProdutoVendido("Caneca de café", 4), new ProdutoVendido("Mouse sem fio", 3)]);
    }

    [Fact]
    public void MaisVendidos_EmpateNaQuantidade_DesempataPeloNome()
    {
        var top = ConsultasPedidos.MaisVendidos(Dados.Pedidos(), Dados.Produtos(), 10);

        top.Select(v => v.Nome).ShouldBe(["Caneca de café", "Mouse sem fio", "Livro Clean Code", "Monitor 27"]);
    }
}
