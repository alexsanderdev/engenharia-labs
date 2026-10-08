using F2M06.TestesUnitarios.Tests.Dubles;
using static F2M06.TestesUnitarios.Tests.Builders.PedidoBuilder;

namespace F2M06.TestesUnitarios.Tests.Infraestrutura;

/// <summary>
/// Passo 3: um fake só é confiável se se comporta como a implementação real.
/// Estes testes são o "contrato" do repositório (na Fase 3, o mesmo contrato roda contra o EF Core + SQL Server).
/// </summary>
public sealed class RepositorioDePedidosEmMemoriaTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AdicionarEObter_DevolveOMesmoPedido()
    {
        var repositorio = new RepositorioDePedidosEmMemoria();
        var pedido = UmPedido().Build();

        await repositorio.AdicionarAsync(pedido, Ct);
        var obtido = await repositorio.ObterPorIdAsync(pedido.Id, Ct);

        obtido.ShouldBeSameAs(pedido);
    }

    [Fact]
    public async Task ObterPorId_IdInexistente_DevolveNull()
    {
        var repositorio = new RepositorioDePedidosEmMemoria(UmPedido());

        var obtido = await repositorio.ObterPorIdAsync(Guid.NewGuid(), Ct);

        obtido.ShouldBeNull();
    }

    [Fact]
    public async Task ContarEmAberto_ContaSoCreatedEConfirmedDoProprioCliente()
    {
        var cliente = Guid.NewGuid();
        var repositorio = new RepositorioDePedidosEmMemoria(
            UmPedido().DoCliente(cliente),
            UmPedido().DoCliente(cliente).Confirmado(),
            UmPedido().DoCliente(cliente).Cancelado(),
            UmPedido().DoCliente(Guid.NewGuid()));

        var emAberto = await repositorio.ContarEmAbertoDoClienteAsync(cliente, Ct);

        emAberto.ShouldBe(2);
    }
}
