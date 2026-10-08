using F4M01.Application.Pedidos;
using F4M01.Domain.Pedidos;
using F4M01.Domain.Produtos;

namespace F4M01.Application.Tests;

/// <summary>A consulta também é caso de uso: e também não pode exigir DbContext para ser testada.</summary>
public sealed class ListarPedidosDoClienteHandlerTests
{
    private static readonly Guid Cliente = Guid.NewGuid();
    private static readonly Guid OutroCliente = Guid.NewGuid();
    private static readonly Produto Cabo = new(Guid.NewGuid(), "Cabo USB", 20m);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly PedidoRepositoryFake _pedidos = new();

    private Pedido Salvar(Guid clienteId, DateTimeOffset criadoEm, int quantidade)
    {
        var pedido = Pedido.Criar(clienteId, criadoEm, [(Cabo, quantidade)]);
        _pedidos.Salvos.Add(pedido);
        return pedido;
    }

    [Fact]
    public async Task Handle_ClienteComPedidos_RetornaSoOsDeleDoMaisRecenteAoMaisAntigo()
    {
        var antigo = Salvar(Cliente, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), 1);
        var recente = Salvar(Cliente, new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), 3);
        Salvar(OutroCliente, new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), 1);
        var handler = Montar.CasoDeUso<ListarPedidosDoClienteHandler>(_pedidos);

        var lista = await handler.HandleAsync(Cliente, Ct);

        lista.Select(p => p.Id).ShouldBe([recente.Id, antigo.Id]);
        lista[0].ShouldBe(new PedidoResumoDto(recente.Id, "Created", 60m, recente.CriadoEm, 1));
    }

    [Fact]
    public async Task Handle_ClienteSemPedidos_RetornaListaVazia()
    {
        var handler = Montar.CasoDeUso<ListarPedidosDoClienteHandler>(_pedidos);

        var lista = await handler.HandleAsync(Guid.NewGuid(), Ct);

        lista.ShouldBeEmpty();
    }
}
