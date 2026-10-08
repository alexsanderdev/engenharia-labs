namespace F4M02.Pedidos.Domain.Tests;

/// <summary>
/// Passos 2 e 3 — O agregado Pedido nasce por um factory method e protege as regras dos itens.
/// </summary>
public sealed class ItensDoPedidoTests
{
    [Fact]
    public void Criar_NovoPedido_NasceEmCreatedSemItensComTotalZero_EAnunciaPedidoCriado()
    {
        var pedido = Pedido.Criar(Dado.Cliente, Dado.Endereco(), "brl", Dado.Agora);

        pedido.Id.ShouldNotBe(default);
        pedido.ClienteId.ShouldBe(Dado.Cliente);
        pedido.EnderecoDeEntrega.ShouldBe(Dado.Endereco());
        pedido.Moeda.ShouldBe("BRL");
        pedido.CriadoEm.ShouldBe(Dado.Agora);
        pedido.Status.ShouldBe(StatusPedido.Created);
        pedido.Itens.ShouldBeEmpty();
        pedido.Total.ShouldBe(Dinheiro.Zero("BRL"));
        pedido.EventosDeDominio.ShouldHaveSingleItem()
            .ShouldBe(new PedidoCriado(pedido.Id, Dado.Cliente, Dado.Agora));

        Should.Throw<ArgumentException>(() => Pedido.Criar(default, Dado.Endereco(), "BRL", Dado.Agora));
        Dado.RegraVioladaPor(() => Pedido.Criar(Dado.Cliente, Dado.Endereco(), "REAIS", Dado.Agora)).ShouldBe(Regras.MoedaInvalida);
    }

    [Fact]
    public void AdicionarItem_CopiaNomeSkuEPrecoDoCatalogo_ECalculaSubtotaisETotal()
    {
        var pedido = Dado.PedidoNovo();
        var cafe = Dado.Produto("CAFE-500G", preco: 10m, nome: "Café 500 g");
        var pao = Dado.Produto("PAO-QUEIJO", preco: 4.5m, nome: "Pão de queijo");

        pedido.AdicionarItem(cafe, new Quantidade(3));
        pedido.AdicionarItem(pao, new Quantidade(2));

        var item = pedido.Itens[0];
        item.ProdutoId.ShouldBe(cafe.Id);
        item.Sku.ShouldBe(new Sku("CAFE-500G"));
        item.NomeDoProduto.ShouldBe("Café 500 g");
        item.PrecoUnitario.ShouldBe(Dinheiro.Reais(10m));
        item.Subtotal.ShouldBe(Dinheiro.Reais(30m));
        pedido.Itens[1].Subtotal.ShouldBe(Dinheiro.Reais(9m));
        pedido.Subtotal.ShouldBe(Dinheiro.Reais(39m));
        pedido.Total.ShouldBe(Dinheiro.Reais(39m));
    }

    [Theory]
    [InlineData(false, "BRL", Regras.ProdutoInativo)]
    [InlineData(true, "USD", Regras.MoedasDiferentes)]
    public void AdicionarItem_ProdutoInativoOuPrecoEmOutraMoeda_EhRejeitado_ENadaMuda(bool ativo, string moeda, string regra)
    {
        var pedido = Dado.PedidoNovo();

        Dado.RegraVioladaPor(() => pedido.AdicionarItem(Dado.Produto(ativo: ativo, moeda: moeda), new Quantidade(1)))
            .ShouldBe(regra);

        pedido.Itens.ShouldBeEmpty();
        pedido.Total.ShouldBe(Dinheiro.Zero("BRL"));
    }

    [Fact]
    public void ItemDoPedido_EhEntidade_MesmoProdutoNaoEntraDuasVezes_QuantidadeMudaNoMesmoItem()
    {
        var pedido = Dado.PedidoNovo();
        var cafe = Dado.Produto(preco: 10m);
        pedido.AdicionarItem(cafe, new Quantidade(1));
        var item = pedido.Itens.ShouldHaveSingleItem();

        Dado.RegraVioladaPor(() => pedido.AdicionarItem(cafe, new Quantidade(2))).ShouldBe(Regras.ProdutoRepetido);

        pedido.AlterarQuantidade(cafe.Id, new Quantidade(5));

        pedido.Itens.ShouldHaveSingleItem().ShouldBeSameAs(item); // mesma entidade, estado novo
        item.Quantidade.ShouldBe(new Quantidade(5));
        pedido.Total.ShouldBe(Dinheiro.Reais(50m));

        pedido.RemoverItem(cafe.Id);
        pedido.Itens.ShouldBeEmpty();
        Dado.RegraVioladaPor(() => pedido.RemoverItem(cafe.Id)).ShouldBe(Regras.ItemNaoEncontrado);
        Dado.RegraVioladaPor(() => pedido.AlterarQuantidade(cafe.Id, new Quantidade(1))).ShouldBe(Regras.ItemNaoEncontrado);
    }

    [Fact]
    public void AdicionarItem_AlemDoLimiteDeItens_EhRejeitado()
    {
        var pedido = Dado.PedidoNovo();
        for (var i = 1; i <= Pedido.MaximoDeItens; i++)
            pedido.AdicionarItem(Dado.Produto($"SKU-{i:000}"), new Quantidade(1));

        Dado.RegraVioladaPor(() => pedido.AdicionarItem(Dado.Produto("SKU-EXTRA"), new Quantidade(1)))
            .ShouldBe(Regras.LimiteDeItens);

        pedido.Itens.Count.ShouldBe(Pedido.MaximoDeItens);
    }

    [Fact]
    public void Agregado_NaoDeixaNinguemDeForaMexerNoEstado()
    {
        var pedido = Dado.PedidoComItem();

        // A coleção exposta é somente leitura: não dá para fazer pedido.Itens.Add(...) nem cast para List.
        pedido.Itens.ShouldBeAssignableTo<ICollection<ItemDoPedido>>()!.IsReadOnly.ShouldBeTrue();
        pedido.Itens.ShouldNotBeOfType<List<ItemDoPedido>>();
        pedido.EventosDeDominio.ShouldBeAssignableTo<ICollection<IEventoDeDominio>>()!.IsReadOnly.ShouldBeTrue();

        // Ninguém cria Pedido nem ItemDoPedido com "new": Pedido nasce pelo factory, item só pela raiz.
        typeof(Pedido).GetConstructors().ShouldBeEmpty();
        typeof(ItemDoPedido).GetConstructors().ShouldBeEmpty();

        // Nenhuma propriedade com setter público: estado muda só por comportamento (métodos com nome do negócio).
        typeof(Pedido).GetProperties().Where(p => p.SetMethod?.IsPublic == true).ShouldBeEmpty();
        typeof(ItemDoPedido).GetProperties().Where(p => p.SetMethod?.IsPublic == true).ShouldBeEmpty();
    }
}
