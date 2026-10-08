using F2M02.Solid.Descontos;
using F2M02.Solid.Dominio;

namespace F2M02.Solid.Tests;

/// <summary>SRP: a regra de criação do pedido testada sozinha — sem banco, SMTP, Kafka ou mocks.</summary>
public class DesignPedidoTests
{
    private static readonly Produto Teclado = new(Guid.NewGuid(), "Teclado", 150m, Ativo: true);
    private static readonly Produto Mouse = new(Guid.NewGuid(), "Mouse", 50m, Ativo: true);

    [Fact]
    public void Criar_LinhasValidas_CalculaSubtotalDescontoETotal()
    {
        var clienteId = Guid.NewGuid();

        var pedido = Pedido.Criar(clienteId, [new(Teclado, 1), new(Mouse, 2)], new DescontoPercentual("P", 0.10m));

        pedido.Id.ShouldNotBe(Guid.Empty);
        pedido.ClienteId.ShouldBe(clienteId);
        pedido.Status.ShouldBe(StatusPedido.Created);
        pedido.Itens.ShouldBe([new ItemPedido(Teclado.Id, "Teclado", 150m, 1), new ItemPedido(Mouse.Id, "Mouse", 50m, 2)]);
        pedido.Subtotal.ShouldBe(250m);
        pedido.Desconto.ShouldBe(25m);
        pedido.Total.ShouldBe(225m);
    }

    [Fact]
    public void Criar_SemLinhas_Lanca()
    {
        Should.Throw<PedidoInvalidoException>(() => Pedido.Criar(Guid.NewGuid(), [], SemDesconto.Instancia))
            .Message.ShouldBe("Pedido precisa de ao menos um item");
    }

    [Fact]
    public void Criar_QuantidadeNaoPositiva_Lanca()
    {
        Should.Throw<PedidoInvalidoException>(() => Pedido.Criar(Guid.NewGuid(), [new(Teclado, 1), new(Mouse, -1)], SemDesconto.Instancia))
            .Message.ShouldBe("Quantidade deve ser maior que zero");
    }

    [Fact]
    public void Criar_ProdutoInativo_Lanca()
    {
        var inativo = Mouse with { Ativo = false };

        Should.Throw<PedidoInvalidoException>(() => Pedido.Criar(Guid.NewGuid(), [new(Teclado, 1), new(inativo, 1)], SemDesconto.Instancia))
            .Message.ShouldBe("Produto inativo: Mouse");
    }
}
