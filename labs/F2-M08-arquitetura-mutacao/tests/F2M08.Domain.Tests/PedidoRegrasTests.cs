using F2M08.Domain.Entidades;

namespace F2M08.Domain.Tests;

/// <summary>
/// Parte 2 (solução): testes que matam os mutantes do Pedido e do Produto.
/// Asserts EXATOS (valor, tipo da exceção, status depois da operação, mensagem quando ela é regra).
/// </summary>
public sealed class PedidoRegrasTests
{
    private static Produto Produto(decimal preco = 200m) => new(Guid.NewGuid(), "Teclado", preco);

    private static Pedido NovoPedido() => new(Guid.NewGuid(), Guid.NewGuid());

    private static Pedido PedidoCom(int quantidade = 1, decimal preco = 200m)
    {
        var pedido = NovoPedido();
        pedido.AdicionarItem(Produto(preco), quantidade);
        return pedido;
    }

    // ---------- Construção ----------

    [Fact]
    public void Construtor_GuardaIdECliente()
    {
        var id = Guid.NewGuid();
        var cliente = Guid.NewGuid();

        var pedido = new Pedido(id, cliente);

        pedido.Id.ShouldBe(id);
        pedido.ClienteId.ShouldBe(cliente);
        pedido.Itens.ShouldBeEmpty();
        pedido.Total.ShouldBe(0m);
    }

    // ---------- Itens e valores ----------

    [Fact]
    public void AdicionarItem_CalculaSubtotalDescontoETotalExatos()
    {
        var pedido = PedidoCom(quantidade: 3, preco: 200m); // 600 → 5% = 30

        pedido.Unidades.ShouldBe(3);
        pedido.Subtotal.ShouldBe(600m);
        pedido.Desconto.ShouldBe(30m);
        pedido.Total.ShouldBe(570m);
        var item = pedido.Itens.ShouldHaveSingleItem();
        item.Quantidade.ShouldBe(3);
        item.PrecoUnitario.ShouldBe(200m);
        item.Subtotal.ShouldBe(600m);
    }

    [Fact]
    public void AdicionarItem_MesmoProdutoDuasVezes_SomaNaMesmaLinha()
    {
        var pedido = NovoPedido();
        var produto = Produto(10m);

        pedido.AdicionarItem(produto, 1);
        pedido.AdicionarItem(produto, 2);

        pedido.Itens.ShouldHaveSingleItem().Quantidade.ShouldBe(3);
        pedido.Subtotal.ShouldBe(30m);
    }

    [Fact]
    public void AdicionarItem_ProdutosDiferentes_CriaLinhasSeparadas()
    {
        var pedido = NovoPedido();

        pedido.AdicionarItem(Produto(10m), 1);
        pedido.AdicionarItem(Produto(20m), 1);

        pedido.Itens.Count.ShouldBe(2);
        pedido.Subtotal.ShouldBe(30m);
    }

    [Fact]
    public void AdicionarItem_QuantidadeZero_LancaArgumentOutOfRange()
    {
        var pedido = NovoPedido();

        var ex = Should.Throw<ArgumentOutOfRangeException>(() => pedido.AdicionarItem(Produto(), 0));

        ex.ParamName.ShouldBe("quantidade");
        pedido.Itens.ShouldBeEmpty();
    }

    [Fact]
    public void AdicionarItem_QuantidadeUm_Aceita()
    {
        PedidoCom(quantidade: 1).Unidades.ShouldBe(1);
    }

    [Fact]
    public void AdicionarItem_ProdutoNulo_LancaArgumentNull()
    {
        Should.Throw<ArgumentNullException>(() => NovoPedido().AdicionarItem(null!, 1));
    }

    [Fact]
    public void AdicionarItem_ProdutoInativo_LancaENaoAdiciona()
    {
        var pedido = NovoPedido();
        var produto = Produto();
        produto.Desativar();

        var ex = Should.Throw<InvalidOperationException>(() => pedido.AdicionarItem(produto, 1));

        ex.Message.ShouldContain("inativo");
        pedido.Itens.ShouldBeEmpty();
    }

    [Fact]
    public void AdicionarItem_PedidoConfirmado_Lanca()
    {
        var pedido = PedidoCom();
        pedido.Confirmar();

        var ex = Should.Throw<InvalidOperationException>(() => pedido.AdicionarItem(Produto(), 1));

        ex.Message.ShouldContain("Created");
    }

    // ---------- Transições ----------

    [Fact]
    public void Confirmar_ComItens_FicaConfirmed()
    {
        var pedido = PedidoCom();

        pedido.Confirmar();

        pedido.Status.ShouldBe(StatusPedido.Confirmed);
    }

    [Fact]
    public void Confirmar_SemItens_Lanca()
    {
        var pedido = NovoPedido();

        var ex = Should.Throw<InvalidOperationException>(pedido.Confirmar);

        ex.Message.ShouldContain("sem itens");
        pedido.Status.ShouldBe(StatusPedido.Created);
    }

    [Fact]
    public void Confirmar_DuasVezes_Lanca()
    {
        var pedido = PedidoCom();
        pedido.Confirmar();

        var ex = Should.Throw<InvalidOperationException>(pedido.Confirmar);

        ex.Message.ShouldBe("Transição inválida: Confirmed → Confirmed.");
    }

    [Fact]
    public void Concluir_Confirmado_FicaCompleted()
    {
        var pedido = PedidoCom();
        pedido.Confirmar();

        pedido.Concluir();

        pedido.Status.ShouldBe(StatusPedido.Completed);
    }

    [Fact]
    public void Concluir_AindaCreated_Lanca()
    {
        var pedido = PedidoCom();

        Should.Throw<InvalidOperationException>(pedido.Concluir);
        pedido.Status.ShouldBe(StatusPedido.Created);
    }

    [Fact]
    public void Cancelar_Completed_LancaComMensagemDeNegocio()
    {
        var pedido = PedidoCom();
        pedido.Confirmar();
        pedido.Concluir();

        var ex = Should.Throw<InvalidOperationException>(pedido.Cancelar);

        ex.Message.ShouldBe("Pedido concluído não pode ser cancelado.");
        pedido.Status.ShouldBe(StatusPedido.Completed);
    }

    [Fact]
    public void Cancelar_Confirmed_Lanca()
    {
        var pedido = PedidoCom();
        pedido.Confirmar();

        Should.Throw<InvalidOperationException>(pedido.Cancelar);
        pedido.Status.ShouldBe(StatusPedido.Confirmed);
    }

    // ---------- Produto ----------

    [Fact]
    public void Produto_Novo_ComecaAtivoComOsDados()
    {
        var id = Guid.NewGuid();

        var produto = new Produto(id, "Mouse", 50m);

        produto.Id.ShouldBe(id);
        produto.Nome.ShouldBe("Mouse");
        produto.Preco.ShouldBe(50m);
        produto.Ativo.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Produto_NomeVazio_Lanca(string nome)
    {
        Should.Throw<ArgumentException>(() => new Produto(Guid.NewGuid(), nome, 10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Produto_PrecoNaoPositivo_Lanca(int preco)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new Produto(Guid.NewGuid(), "Mouse", preco));
    }

    [Fact]
    public void Produto_AlterarPreco_TrocaOPrecoEValida()
    {
        var produto = new Produto(Guid.NewGuid(), "Mouse", 50m);

        produto.AlterarPreco(60m);

        produto.Preco.ShouldBe(60m);
        Should.Throw<ArgumentOutOfRangeException>(() => produto.AlterarPreco(0m));
        produto.Preco.ShouldBe(60m);
    }

    [Fact]
    public void Produto_DesativarEAtivar_AlternamOStatus()
    {
        var produto = new Produto(Guid.NewGuid(), "Mouse", 50m);

        produto.Desativar();
        produto.Ativo.ShouldBeFalse();

        produto.Ativar();
        produto.Ativo.ShouldBeTrue();
    }
}
