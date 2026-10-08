using Microsoft.Extensions.Time.Testing;

using MP2.OrderCalc.Aplicacao;
using MP2.OrderCalc.Dominio;

namespace MP2.OrderCalc.Tests;

/// <summary>
/// Prova de que os seams funcionam: o serviço roda sem o Db estático,
/// com fakes em memória e relógio controlado.
/// </summary>
public class ServicoDePedidosTests
{
    private sealed class Fakes : ICatalogoDeProdutos, ICadastroDeClientes, ICupons, IRegistroDePedidos
    {
        public Dictionary<string, ProdutoCatalogo> Produtos { get; } = new()
        {
            ["P1"] = new ProdutoCatalogo("P1", Money.Reais(100m), 1, false, true, 2),
        };

        public List<PedidoFechado> Gravados { get; } = [];

        public ProdutoCatalogo? Buscar(string sku) => Produtos.GetValueOrDefault(sku);

        ClientePedido? ICadastroDeClientes.Buscar(string id) => id == "VIP" ? new ClientePedido(id, TipoCliente.Vip, false) : null;

        CupomPromocional? ICupons.Buscar(string codigo) => null;

        public string Registrar(PedidoFechado pedido)
        {
            Gravados.Add(pedido);
            return "N-" + Gravados.Count;
        }
    }

    private static (ServicoDePedidos Servico, Fakes Fakes) Criar()
    {
        var fakes = new Fakes();
        var servico = new ServicoDePedidos(fakes, fakes, fakes, fakes, new FakeTimeProvider(Cenarios.DataPadrao));
        return (servico, fakes);
    }

    [Fact]
    public void Calcular_ClienteVipSemDb_AplicaDescontoEFreteGratis()
    {
        var (servico, _) = Criar();
        var cliente = servico.ObterCliente("VIP", ModoDeCalculo.Pedido);
        var item = servico.ResolverItem("P1", Quantidade.Criar(2), ModoDeCalculo.Pedido);
        var pedido = new PedidoParaCalcular(cliente, [item], null, Uf.Ler("SP"), TipoEntrega.Normal);

        var calculado = servico.Calcular(pedido, ModoDeCalculo.Pedido, servico.Agora());

        calculado.Desconto.ShouldBe(Money.Reais(10m));
        calculado.Frete.ShouldBe(Money.Zero);
        calculado.Total.ShouldBe(Money.Reais(226m)); // 200 - 10 + 36 de imposto (18% de 200)
    }

    [Fact]
    public void ResolverItem_EstoqueInsuficienteSoNoPedido()
    {
        var (servico, _) = Criar();

        Should.Throw<PedidoInvalidoException>(() => servico.ResolverItem("P1", Quantidade.Criar(3), ModoDeCalculo.Pedido))
            .Message.ShouldBe("Estoque insuficiente: P1");
        servico.ResolverItem("P1", Quantidade.Criar(3), ModoDeCalculo.Orcamento).Quantidade.Valor.ShouldBe(3);
    }

    [Fact]
    public void FachadaComServicoInjetado_GravaNoRegistroFake()
    {
        var (servico, fakes) = Criar();

        var r = new OrderCalculator(servico).Calcular("VIP", "p1:1", null!, "SP", "NORMAL");

        r.Numero.ShouldBe("N-1");
        fakes.Gravados.Single().Total.ShouldBe(Money.Reais(113m)); // 100 - 5 + 18
    }
}
