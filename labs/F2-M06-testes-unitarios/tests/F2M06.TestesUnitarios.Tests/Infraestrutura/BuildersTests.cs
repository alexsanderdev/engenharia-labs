using F2M06.TestesUnitarios.Dominio;
using static F2M06.TestesUnitarios.Tests.Builders.PedidoBuilder;
using static F2M06.TestesUnitarios.Tests.Builders.ProdutoBuilder;

namespace F2M06.TestesUnitarios.Tests.Infraestrutura;

/// <summary>
/// Passos 1 e 2: a infraestrutura de teste também é código — e também tem testes.
/// Estes testes garantem que os builders entregam objetos VÁLIDOS por padrão e respeitam cada ajuste.
/// </summary>
public sealed class BuildersTests
{
    [Fact]
    public void ProdutoBuilder_SemAjustes_CriaProdutoAtivoEValido()
    {
        Produto produto = UmProduto();

        produto.Id.ShouldNotBe(Guid.Empty);
        produto.Nome.ShouldNotBeNullOrWhiteSpace();
        produto.Preco.ShouldBeGreaterThan(0m);
        produto.Ativo.ShouldBeTrue();
    }

    [Fact]
    public void ProdutoBuilder_ComNomePrecoEId_AplicaOsValores()
    {
        var id = Guid.NewGuid();

        var produto = UmProduto().ComId(id).ComNome("Refrigerante").ComPreco(8.5m).Build();

        produto.Id.ShouldBe(id);
        produto.Nome.ShouldBe("Refrigerante");
        produto.Preco.ShouldBe(8.5m);
    }

    [Fact]
    public void ProdutoBuilder_Inativo_CriaProdutoInativo()
    {
        var produto = UmProduto().Inativo().Build();

        produto.Ativo.ShouldBeFalse();
    }

    [Fact]
    public void PedidoBuilder_SemAjustes_CriaPedidoCreatedComUmItemNaDataPadrao()
    {
        Pedido pedido = UmPedido();

        pedido.Status.ShouldBe(StatusPedido.Created);
        pedido.Itens.ShouldHaveSingleItem().Quantidade.ShouldBe(1);
        pedido.CriadoEm.ShouldBe(DataPadrao);
        pedido.ClienteId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void PedidoBuilder_ComItens_UsaSomenteOsItensInformados()
    {
        var clienteId = Guid.NewGuid();

        var pedido = UmPedido()
            .DoCliente(clienteId)
            .ComItem(UmProduto().ComPreco(10m), 2)
            .ComItem(UmProduto().ComPreco(5m), 1)
            .Build();

        pedido.ClienteId.ShouldBe(clienteId);
        pedido.Itens.Count.ShouldBe(2);
        pedido.Total.ShouldBe(25m);
    }

    [Fact]
    public void PedidoBuilder_Confirmado_CriaPedidoConfirmed()
    {
        var pedido = UmPedido().Confirmado().Build();

        pedido.Status.ShouldBe(StatusPedido.Confirmed);
    }

    [Fact]
    public void PedidoBuilder_Cancelado_CriaPedidoCancelled()
    {
        var pedido = UmPedido().Cancelado().Build();

        pedido.Status.ShouldBe(StatusPedido.Cancelled);
    }
}
