namespace F1M01.CSharpModerno.Tests;

// Passo 3 — StatusPedido: switch expressions com tuple patterns.
public class StatusPedidoTests
{
    [Theory]
    [InlineData(StatusPedido.Criado, StatusPedido.Confirmado, true)]
    [InlineData(StatusPedido.Confirmado, StatusPedido.Concluido, true)]
    [InlineData(StatusPedido.Criado, StatusPedido.Cancelado, true)]
    [InlineData(StatusPedido.Concluido, StatusPedido.Cancelado, false)]
    [InlineData(StatusPedido.Criado, StatusPedido.Concluido, false)]
    [InlineData(StatusPedido.Cancelado, StatusPedido.Confirmado, false)]
    [InlineData(StatusPedido.Criado, StatusPedido.Criado, false)]
    public void PodeTransicionar_RespeitaMaquinaDeEstados(StatusPedido de, StatusPedido para, bool esperado)
    {
        TransicoesDePedido.PodeTransicionar(de, para).ShouldBe(esperado);
    }

    [Fact]
    public void Descrever_RetornaTextoOuLancaParaValorDesconhecido()
    {
        TransicoesDePedido.Descrever(StatusPedido.Criado).ShouldBe("Aguardando confirmação");
        TransicoesDePedido.Descrever(StatusPedido.Confirmado).ShouldBe("Em preparo");
        TransicoesDePedido.Descrever(StatusPedido.Concluido).ShouldBe("Entregue");
        TransicoesDePedido.Descrever(StatusPedido.Cancelado).ShouldBe("Cancelado");
        Should.Throw<ArgumentOutOfRangeException>(() => TransicoesDePedido.Descrever((StatusPedido)99));
    }
}

// Passo 4 — Pedido: primary constructor, regras de item e de status.
public class PedidoTests
{
    private static readonly Produto Burger = new() { Nome = "X-Burger", Preco = Dinheiro.Reais(30m) };

    [Fact]
    public void AdicionarItem_CalculaSubtotal()
    {
        var pedido = new Pedido(new Cliente("Ana"));

        pedido.Subtotal.ShouldBe(Dinheiro.Reais(0m));
        pedido.AdicionarItem(Burger, 2);
        pedido.AdicionarItem(Burger with { Preco = Dinheiro.Reais(5.5m) }, 1);

        pedido.Itens.Count.ShouldBe(2);
        pedido.Subtotal.ShouldBe(Dinheiro.Reais(65.5m));
    }

    [Fact]
    public void AdicionarItem_EntradasInvalidas_Lancam()
    {
        var pedido = new Pedido(new Cliente("Ana"));

        Should.Throw<ArgumentOutOfRangeException>(() => pedido.AdicionarItem(Burger, 0));
        Should.Throw<InvalidOperationException>(() => pedido.AdicionarItem(Burger.Desativar(), 1));
        pedido.Itens.ShouldBeEmpty();
    }

    [Fact]
    public void AlterarStatus_TransicaoInvalidaLancaEPedidoConfirmadoNaoAceitaItens()
    {
        var pedido = new Pedido(new Cliente("Ana"));
        pedido.AdicionarItem(Burger, 1);

        pedido.AlterarStatus(StatusPedido.Confirmado);

        pedido.Status.ShouldBe(StatusPedido.Confirmado);
        Should.Throw<InvalidOperationException>(() => pedido.AdicionarItem(Burger, 1));
        pedido.AlterarStatus(StatusPedido.Concluido);
        Should.Throw<InvalidOperationException>(() => pedido.AlterarStatus(StatusPedido.Cancelado));
        pedido.Status.ShouldBe(StatusPedido.Concluido);
    }
}

// Passo 5 — Regras de desconto: property, relational e list patterns.
public class RegrasDeDescontoTests
{
    private static Pedido Montar(bool vip, params (decimal preco, int qtd)[] itens)
    {
        var pedido = new Pedido(new Cliente("Cliente", vip));
        var i = 0;
        foreach (var (preco, qtd) in itens)
            pedido.AdicionarItem(new Produto { Nome = $"P{i++}", Preco = Dinheiro.Reais(preco) }, qtd);
        return pedido;
    }

    [Fact]
    public void PercentualPara_PedidoVazioOuSimples_SemDesconto()
    {
        RegrasDeDesconto.PercentualPara(Montar(vip: false)).ShouldBe(0m);
        RegrasDeDesconto.PercentualPara(Montar(vip: true)).ShouldBe(0m);
        RegrasDeDesconto.PercentualPara(Montar(false, (20m, 1), (10m, 2))).ShouldBe(0m);
    }

    [Fact]
    public void PercentualPara_VipESubtotalAlto_UsaRelationalEPropertyPatterns()
    {
        RegrasDeDesconto.PercentualPara(Montar(true, (100m, 5))).ShouldBe(0.15m);
        RegrasDeDesconto.PercentualPara(Montar(true, (100m, 1))).ShouldBe(0.10m);
        RegrasDeDesconto.PercentualPara(Montar(false, (250m, 2))).ShouldBe(0.05m);
    }

    [Fact]
    public void PercentualPara_AtacadoETresItens_UsaListPatterns()
    {
        RegrasDeDesconto.PercentualPara(Montar(false, (5m, 10))).ShouldBe(0.08m);
        RegrasDeDesconto.PercentualPara(Montar(false, (5m, 10), (5m, 1))).ShouldBe(0m);
        RegrasDeDesconto.PercentualPara(Montar(false, (5m, 1), (5m, 1), (5m, 1))).ShouldBe(0.03m);
    }

    [Fact]
    public void TotalComDesconto_AplicaPercentualNoSubtotal()
    {
        RegrasDeDesconto.TotalComDesconto(Montar(true, (100m, 5))).ShouldBe(Dinheiro.Reais(425m));
        RegrasDeDesconto.TotalComDesconto(Montar(false, (10m, 1))).ShouldBe(Dinheiro.Reais(10m));
    }
}

// Passo 6 — Catálogo: nullable reference types e collection expressions.
public class CatalogoTests
{
    private static readonly Produto Burger = new() { Nome = "X-Burger", Preco = Dinheiro.Reais(30m) };
    private static readonly Produto Suco = new() { Nome = "Suco", Preco = Dinheiro.Reais(8m), Ativo = false };

    [Fact]
    public void BuscarPorNome_IgnoraCaixaERetornaNullQuandoNaoExiste()
    {
        var catalogo = new Catalogo([Burger, Suco]);

        catalogo.BuscarPorNome("x-burger").ShouldBe(Burger);
        catalogo.BuscarPorNome("Pizza").ShouldBeNull();
        catalogo.NomeOuPadrao("SUCO").ShouldBe("Suco");
        catalogo.NomeOuPadrao("Pizza").ShouldBe("(produto não encontrado)");
    }

    [Fact]
    public void Ativos_RetornaSnapshotSomenteComAtivos()
    {
        var catalogo = new Catalogo([Burger, Suco]);

        var ativos = catalogo.Ativos();
        catalogo.Adicionar(new Produto { Nome = "Pizza", Preco = Dinheiro.Reais(50m) });

        ativos.ShouldBe([Burger]);
        catalogo.Ativos().Count.ShouldBe(2);
        catalogo.Quantidade.ShouldBe(3);
    }

    [Fact]
    public void Mesclar_JuntaSequenciasNaOrdem()
    {
        var pizza = new Produto { Nome = "Pizza", Preco = Dinheiro.Reais(50m) };

        Catalogo.Mesclar([Burger], [Suco, pizza]).ShouldBe([Burger, Suco, pizza]);
        Catalogo.Mesclar([], []).ShouldBeEmpty();
    }
}
