using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Contratos;
using F6M02.ServiceBus.Sessoes;
using F6M02.ServiceBus.Tests.Infra;
using F6M02.ServiceBus.Topologia;

namespace F6M02.ServiceBus.Tests;

/// <summary>
/// Passo 6 — sessions: ordem garantida por pedido (SessionId = PedidoId), pedidos diferentes em paralelo,
/// e session state para reconhecer reentregas.
/// </summary>
[Collection(ColecaoServiceBus.Nome)]
public sealed class Passo6_SessoesTests(ServiceBusFixture fixture)
{
    [Fact]
    public async Task Processar_EventosIntercaladosDeTresPedidos_CadaPedidoEmOrdemEReentregaIgnorada()
    {
        // O contrato da mensagem e as opções do processor primeiro (sem broker).
        var pedidoId = Guid.NewGuid();
        var mensagem = PublicadorDeEventosDoPedido.CriarMensagem(new EventoDoPedido(pedidoId, 2, "PedidoPago"));

        mensagem.SessionId.ShouldBe(pedidoId.ToString("N"));
        mensagem.MessageId.ShouldBe($"{pedidoId:N}-2");
        mensagem.Subject.ShouldBe("PedidoPago");
        mensagem.ContentType.ShouldBe("application/json");

        var opcoes = ProcessadorDeEventosDoPedido.CriarOpcoes(maxConcurrentSessions: 3);
        opcoes.MaxConcurrentSessions.ShouldBe(3);
        opcoes.MaxConcurrentCallsPerSession.ShouldBe(1, "mais de 1 por sessão quebraria a ordem dentro do pedido");
        opcoes.AutoCompleteMessages.ShouldBeFalse();
        opcoes.ReceiveMode.ShouldBe(ServiceBusReceiveMode.PeekLock);

        string[] tipos = ["PedidoCriado", "PedidoPago", "PedidoSeparado", "PedidoEnviado", "PedidoEntregue"];
        var pedidos = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        await using (var publicador = new PublicadorDeEventosDoPedido(fixture.Cliente))
        {
            // Intercalados: A1 B1 C1 A2 B2 C2 ...
            for (var seq = 1; seq <= tipos.Length; seq++)
                foreach (var pedido in pedidos)
                    await publicador.PublicarAsync(new EventoDoPedido(pedido, seq, tipos[seq - 1]));

            // Reentrega atrasada do evento 3 do primeiro pedido (ex.: o publicador repetiu depois de um timeout).
            await publicador.PublicarAsync(new EventoDoPedido(pedidos[0], 3, tipos[2]));
        }

        var vistos = new ConcurrentDictionary<Guid, ConcurrentQueue<int>>();
        var sessoesErradas = new ConcurrentQueue<string>();
        var total = 0;

        await using var processador = new ProcessadorDeEventosDoPedido(fixture.Cliente, (evento, ctx, ct) =>
        {
            if (ctx.SessionId != evento.PedidoId.ToString("N")) sessoesErradas.Enqueue($"{ctx.SessionId} x {evento.PedidoId}");
            vistos.GetOrAdd(evento.PedidoId, _ => new()).Enqueue(evento.Sequencia);
            Interlocked.Increment(ref total);
            return Task.CompletedTask;
        }, maxConcurrentSessions: 3);
        await processador.IniciarAsync();

        await Esperas.Eventualmente(
            () => Task.FromResult(Volatile.Read(ref total) >= 15 && processador.Ignorados >= 1),
            "15 eventos processados e 1 reentrega ignorada", TimeSpan.FromSeconds(30));
        await processador.PararAsync();

        sessoesErradas.ShouldBeEmpty();
        foreach (var pedido in pedidos)
            vistos[pedido].ToArray().ShouldBe([1, 2, 3, 4, 5], $"eventos do pedido {pedido:N} fora de ordem ou repetidos");
        processador.Ignorados.ShouldBe(1);
        total.ShouldBe(15);
    }
}
