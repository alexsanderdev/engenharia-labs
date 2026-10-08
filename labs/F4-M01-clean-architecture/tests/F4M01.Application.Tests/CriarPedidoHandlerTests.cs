using F4M01.Application.Pedidos;
using F4M01.Domain.Comum;
using F4M01.Domain.Pedidos;
using F4M01.Domain.Produtos;

namespace F4M01.Application.Tests;

/// <summary>
/// Caso de uso "Criar pedido" testado SEM banco, SEM web, SEM mocks de framework:
/// só o handler, o domínio de verdade e fakes das portas.
/// </summary>
public sealed class CriarPedidoHandlerTests
{
    private static readonly Guid ClienteId = Guid.Parse("c1c1c1c1-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Agora = new(2026, 3, 10, 14, 30, 0, TimeSpan.Zero);

    private static readonly Produto Teclado = new(Guid.NewGuid(), "Teclado", 150.00m);
    private static readonly Produto Mouse = new(Guid.NewGuid(), "Mouse", 80.50m);
    private static readonly Produto Monitor = new(Guid.NewGuid(), "Monitor CRT", 300.00m, ativo: false);

    private readonly PedidoRepositoryFake _pedidos = new();
    private readonly ProdutoRepositoryFake _produtos = new(Teclado, Mouse, Monitor);
    private readonly RelogioFake _relogio = new(Agora);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private CriarPedidoHandler CriarHandler() => Montar.CasoDeUso<CriarPedidoHandler>(_pedidos, _produtos, _relogio);

    private static CriarPedidoCommand Comando(params (Guid ProdutoId, int Quantidade)[] itens) =>
        new(ClienteId, [.. itens.Select(i => new ItemDoPedidoCommand(i.ProdutoId, i.Quantidade))]);

    [Fact]
    public void Handler_DependeSoDasPortas()
    {
        Should.NotThrow(CriarHandler);
    }

    [Fact]
    public async Task Handle_PedidoValido_SalvaUmPedidoNoRepositorio()
    {
        var dto = await CriarHandler().HandleAsync(Comando((Teclado.Id, 1)), Ct);

        var salvo = _pedidos.Salvos.ShouldHaveSingleItem();
        salvo.Id.ShouldBe(dto.Id);
        salvo.ClienteId.ShouldBe(ClienteId);
        salvo.Status.ShouldBe(StatusPedido.Created);
    }

    [Fact]
    public async Task Handle_PedidoValido_CalculaTotalComPrecoDoCatalogo()
    {
        var dto = await CriarHandler().HandleAsync(Comando((Teclado.Id, 2), (Mouse.Id, 3)), Ct);

        dto.Total.ShouldBe(2 * 150.00m + 3 * 80.50m);
        var salvo = _pedidos.Salvos.ShouldHaveSingleItem();
        salvo.Itens.Count.ShouldBe(2);
        salvo.Itens.ShouldContain(i => i.ProdutoId == Mouse.Id && i.PrecoUnitario == 80.50m && i.Quantidade == 3);
    }

    [Fact]
    public async Task Handle_PedidoValido_UsaOHorarioDoRelogio()
    {
        var dto = await CriarHandler().HandleAsync(Comando((Teclado.Id, 1)), Ct);

        dto.CriadoEm.ShouldBe(Agora);
        _pedidos.Salvos.ShouldHaveSingleItem().CriadoEm.ShouldBe(Agora);
    }

    [Fact]
    public async Task Handle_SemItens_LancaDomainExceptionENaoSalva()
    {
        var ex = await Should.ThrowAsync<DomainException>(() => CriarHandler().HandleAsync(Comando(), Ct));

        ex.Message.ShouldContain("pelo menos um item");
        _pedidos.Salvos.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Handle_QuantidadeNaoPositiva_LancaDomainExceptionENaoSalva(int quantidade)
    {
        var ex = await Should.ThrowAsync<DomainException>(
            () => CriarHandler().HandleAsync(Comando((Teclado.Id, quantidade)), Ct));

        ex.Message.ShouldContain("maior que zero");
        _pedidos.Salvos.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ProdutoInativo_LancaDomainExceptionComNomeDoProduto()
    {
        var ex = await Should.ThrowAsync<DomainException>(
            () => CriarHandler().HandleAsync(Comando((Teclado.Id, 1), (Monitor.Id, 1)), Ct));

        ex.Message.ShouldContain("Monitor CRT");
        _pedidos.Salvos.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ProdutoInexistente_LancaProdutoNaoEncontrado()
    {
        var inexistente = Guid.NewGuid();

        var ex = await Should.ThrowAsync<ProdutoNaoEncontradoException>(
            () => CriarHandler().HandleAsync(Comando((Teclado.Id, 1), (inexistente, 1)), Ct));

        ex.ProdutoId.ShouldBe(inexistente);
        _pedidos.Salvos.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ProdutoRepetido_ConsultaOCatalogoUmaVezComIdsDistintos()
    {
        await CriarHandler().HandleAsync(Comando((Teclado.Id, 1), (Mouse.Id, 1), (Teclado.Id, 2)), Ct);

        var consulta = _produtos.Consultas.ShouldHaveSingleItem();
        consulta.ShouldBe([Teclado.Id, Mouse.Id], ignoreOrder: true);
    }
}
