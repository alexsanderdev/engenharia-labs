using System.Net;
using F4M06.Integracao.Tests.Infra;
using F4M06.Pedidos.Contracts;
using F4M06.Shared.Eventos;
using Microsoft.Extensions.DependencyInjection;

namespace F4M06.Integracao.Tests;

/// <summary>
/// Comunicação por EVENTOS em processo: Pedidos publica <see cref="PedidoConfirmado"/> e Clientes reage
/// (total gasto e fidelidade). Pedidos não conhece Clientes; Clientes não lê as tabelas de Pedidos.
/// </summary>
[Collection(ColecaoIntegracao.Nome)]
public sealed class EventosDeIntegracaoTests(OrderFlowFixture fixture) : IntegracaoTestBase(fixture)
{
    [Fact]
    public async Task ConfirmarPedido_PublicaPedidoConfirmado_ClienteAcumulaTotalGasto()
    {
        var cliente = await CriarClienteAsync();
        var produto = await CriarProdutoAsync("Moedor", 300m);
        var pedido = await CriarPedidoAsync(cliente.Id, (produto.Id, 2));

        (await ConfirmarPedidoAsync(pedido.Id)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var lido = await ObterClienteAsync(cliente.Id);
        lido.TotalGasto.ShouldBe(600m);
        lido.PedidosConfirmados.ShouldBe(1);
        lido.Nivel.ShouldBe("Bronze");
    }

    [Fact]
    public async Task PedidosConfirmadosSomandoMilOuMais_ClienteSobeParaPrata()
    {
        var cliente = await CriarClienteAsync();
        var produto = await CriarProdutoAsync("Cafeteira", 400m);
        var primeiro = await CriarPedidoAsync(cliente.Id, (produto.Id, 2));
        var segundo = await CriarPedidoAsync(cliente.Id, (produto.Id, 1));

        await ConfirmarPedidoAsync(primeiro.Id);
        (await ObterClienteAsync(cliente.Id)).Nivel.ShouldBe("Bronze");
        await ConfirmarPedidoAsync(segundo.Id);

        var lido = await ObterClienteAsync(cliente.Id);
        lido.TotalGasto.ShouldBe(1_200m);
        lido.PedidosConfirmados.ShouldBe(2);
        lido.Nivel.ShouldBe("Prata");
    }

    [Fact]
    public async Task PedidoConfirmado_EntregueDuasVezes_ContabilizaUmaVezSo()
    {
        var cliente = await CriarClienteAsync();
        var evento = new PedidoConfirmado(Guid.NewGuid(), DateTimeOffset.UtcNow, PedidoId: Guid.NewGuid(), cliente.Id, Total: 250m);

        // Entrega "pelo menos uma vez" pode repetir: o assinante tem que ser idempotente.
        var barramento = Fixture.Factory.Services.GetRequiredService<IEventBus>();
        await barramento.PublishAsync(evento, Ct);
        await barramento.PublishAsync(evento with { EventId = Guid.NewGuid() }, Ct);

        var lido = await ObterClienteAsync(cliente.Id);
        lido.TotalGasto.ShouldBe(250m);
        lido.PedidosConfirmados.ShouldBe(1);
    }
}
