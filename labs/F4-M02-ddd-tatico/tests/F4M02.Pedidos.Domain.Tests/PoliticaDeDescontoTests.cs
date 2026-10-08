namespace F4M02.Pedidos.Domain.Tests;

/// <summary>
/// Passo 6 — Domain service: a política de desconto depende do cliente, que não pertence ao agregado Pedido.
/// </summary>
public sealed class PoliticaDeDescontoTests
{
    private readonly PoliticaDeDesconto _politica = new();

    [Theory]
    [InlineData(0, false, 200, 0)]       // cliente comum
    [InlineData(4, false, 200, 0)]       // ainda não é fiel
    [InlineData(5, false, 200, 10)]      // fiel: 5%
    [InlineData(0, true, 200, 20)]       // VIP: 10%
    [InlineData(12, true, 200, 20)]      // VIP e fiel não acumulam: vale o maior
    [InlineData(0, true, 800, 50)]       // VIP: 80 → teto de 50
    [InlineData(5, false, 33.33, 1.67)]  // 1,6665 → 1,67
    public void Desconto_DependeDoPerfilDoCliente(int pedidosConcluidos, bool vip, decimal subtotal, decimal descontoEsperado)
    {
        var pedido = Dado.PedidoComItem(preco: subtotal);
        var perfil = new PerfilDoCliente(Dado.Cliente, pedidosConcluidos, vip);

        var desconto = _politica.Aplicar(pedido, perfil);

        desconto.ShouldBe(Dinheiro.Reais(descontoEsperado));
        pedido.Desconto.ShouldBe(Dinheiro.Reais(descontoEsperado));
        pedido.Subtotal.ShouldBe(Dinheiro.Reais(subtotal));
        pedido.Total.ShouldBe(Dinheiro.Reais(subtotal - descontoEsperado));
    }

    [Fact]
    public void Desconto_NuncaDeixaOPedidoInconsistente()
    {
        var pedido = Dado.PedidoComItem(preco: 100m);

        // O perfil precisa ser do dono do pedido.
        var perfilDeOutro = new PerfilDoCliente(ClienteId.Novo(), 50, true);
        Dado.RegraVioladaPor(() => _politica.Aplicar(pedido, perfilDeOutro)).ShouldBe(Regras.ClienteDiferente);
        pedido.Desconto.ShouldBe(Dinheiro.Zero("BRL"));

        // O agregado não aceita desconto maior que o subtotal, nem em outra moeda...
        Dado.RegraVioladaPor(() => pedido.AplicarDesconto(Dinheiro.Reais(100.01m))).ShouldBe(Regras.DescontoInvalido);
        Dado.RegraVioladaPor(() => pedido.AplicarDesconto(new Dinheiro(1m, "USD"))).ShouldBe(Regras.MoedasDiferentes);

        // ...e, quando os itens mudam, o desconto calculado sobre o subtotal antigo deixa de valer.
        _politica.Aplicar(pedido, new PerfilDoCliente(Dado.Cliente, 0, true));
        pedido.Total.ShouldBe(Dinheiro.Reais(90m));

        pedido.RemoverItem(pedido.Itens[0].ProdutoId);

        pedido.Desconto.ShouldBe(Dinheiro.Zero("BRL"));
        pedido.Total.ShouldBe(Dinheiro.Zero("BRL"));
    }
}
