using F4M04.Cqrs.Pedidos.Dominio;
using F4M04.Cqrs.Pedidos.Leitura;

namespace F4M04.Cqrs.Tests;

/// <summary>Passo 7: a projeção, testada sem DI (é só uma classe que recebe eventos).</summary>
public sealed class ProjecaoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Projecao_CriadoDepoisConfirmado_MontaEAtualizaOResumo()
    {
        var leitura = new BancoDeLeitura();
        var projecao = new ProjecaoDePedidos(leitura);
        var pedidoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();

        await projecao.HandleAsync(new PedidoCriado(pedidoId, clienteId, 490.00m, 3, Agora), Ct);
        leitura.Pedidos[pedidoId].ShouldBe(new PedidoResumo(pedidoId, clienteId, "Created", 490.00m, 3, Agora, null));

        await projecao.HandleAsync(new PedidoConfirmado(pedidoId, Agora.AddMinutes(30)), Ct);
        leitura.Pedidos[pedidoId].ShouldBe(new PedidoResumo(pedidoId, clienteId, "Confirmed", 490.00m, 3, Agora, Agora.AddMinutes(30)));
    }

    [Fact]
    public async Task Projecao_EventoRepetidoOuDePedidoDesconhecido_NaoDuplicaNemQuebra()
    {
        var leitura = new BancoDeLeitura();
        var projecao = new ProjecaoDePedidos(leitura);
        var criado = new PedidoCriado(Guid.NewGuid(), Guid.NewGuid(), 120.00m, 1, Agora);

        await projecao.HandleAsync(criado, Ct);
        await projecao.HandleAsync(criado, Ct);
        await projecao.HandleAsync(new PedidoConfirmado(Guid.NewGuid(), Agora), Ct);

        leitura.Pedidos.Count.ShouldBe(1);
        leitura.Pedidos[criado.PedidoId].Status.ShouldBe("Created");
    }
}
