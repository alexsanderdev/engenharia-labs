using F2M06.TestesUnitarios.Dominio;
using static F2M06.TestesUnitarios.Tests.Builders.PedidoBuilder;
using static F2M06.TestesUnitarios.Tests.Builders.ProdutoBuilder;

namespace F2M06.TestesUnitarios.Tests.Dominio;

/// <summary>
/// Passo 4: testes de DOMÍNIO. Sem mocks: Pedido e Produto são objetos de verdade.
/// Repare como o builder deixa cada teste dizer só o que importa para ele.
/// </summary>
public sealed class PedidoTests
{
    [Fact]
    public void Criar_SemItens_LancaRegraDeNegocio()
    {
        var criar = () => UmPedido().SemItens().Build();

        Should.Throw<RegraDeNegocioException>(criar).Message.ShouldContain("ao menos um item");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Criar_QuantidadeMenorOuIgualAZero_LancaRegraDeNegocio(int quantidade)
    {
        var criar = () => UmPedido().ComItem(UmProduto(), quantidade).Build();

        Should.Throw<RegraDeNegocioException>(criar).Message.ShouldContain("maior que zero");
    }

    [Fact]
    public void Criar_ComProdutoInativo_LancaRegraDeNegocio()
    {
        var inativo = UmProduto().ComNome("Pizza Calabresa").Inativo();

        var criar = () => UmPedido().ComItem(inativo).Build();

        Should.Throw<RegraDeNegocioException>(criar).Message.ShouldBe("Produto 'Pizza Calabresa' está inativo.");
    }

    public static TheoryData<decimal, int, decimal, int, decimal> Totais => new()
    {
        { 50m, 1, 8m, 1, 58m },
        { 50m, 2, 8m, 3, 124m },
        { 0.10m, 3, 0.20m, 1, 0.50m }, // decimal: sem erro de arredondamento de double
    };

    [Theory]
    [MemberData(nameof(Totais))]
    public void Total_SomaPrecoVezesQuantidadeDeCadaItem(decimal preco1, int qtd1, decimal preco2, int qtd2, decimal esperado)
    {
        var pedido = UmPedido()
            .ComItem(UmProduto().ComPreco(preco1), qtd1)
            .ComItem(UmProduto().ComPreco(preco2), qtd2)
            .Build();

        pedido.Total.ShouldBe(esperado);
    }

    [Fact]
    public void Criar_MesmoProdutoDuasVezes_AgrupaEmUmaLinhaSomandoAsQuantidades()
    {
        Produto pizza = UmProduto().ComPreco(50m);

        var pedido = UmPedido().ComItem(pizza, 1).ComItem(pizza, 2).Build();

        var item = pedido.Itens.ShouldHaveSingleItem();
        item.Quantidade.ShouldBe(3);
        pedido.Total.ShouldBe(150m);
    }

    [Fact]
    public void ExpiraEm_EhTrintaMinutosDepoisDaCriacao()
    {
        var criadoEm = new DateTimeOffset(2026, 3, 10, 18, 0, 0, TimeSpan.Zero);

        var pedido = UmPedido().CriadoEm(criadoEm).Build();

        pedido.ExpiraEm.ShouldBe(criadoEm.AddMinutes(30));
    }

    [Theory]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(45, true)]
    public void EstaExpirado_PedidoCreated_DependeDoTempoDecorrido(int minutosDepois, bool esperado)
    {
        var pedido = UmPedido().Build();

        var expirado = pedido.EstaExpirado(DataPadrao.AddMinutes(minutosDepois));

        expirado.ShouldBe(esperado);
    }

    [Fact]
    public void EstaExpirado_PedidoConfirmado_NuncaExpira()
    {
        var pedido = UmPedido().Confirmado().Build();

        pedido.EstaExpirado(DataPadrao.AddDays(10)).ShouldBeFalse();
    }

    [Fact]
    public void Confirmar_PedidoCancelado_LancaRegraDeNegocio()
    {
        var pedido = UmPedido().Cancelado().Build();

        Should.Throw<RegraDeNegocioException>(pedido.Confirmar);
    }
}
