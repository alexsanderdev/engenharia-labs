using F6M05.Sagas.Coreografia;
using F6M05.Sagas.Mensageria;
using F6M05.Sagas.Retry;
using F6M05.Sagas.Saga;
using F6M05.Tests.Infra;
using RabbitMQ.Client;

namespace F6M05.Tests.Parte3_Coreografia;

/// <summary>
/// Passo 10 — o mesmo fluxo SEM orquestrador: Estoque e Pagamento reagem a eventos num exchange
/// topic. Repare que, para saber "em que pé está o pedido", o teste precisa espiar TODOS os eventos.
/// </summary>
public sealed class CoreografiaTests(InfraFixture infra)
{
    private static readonly PoliticaDeRetry Politica = new()
    {
        RetentativasImediatas = 1,
        Atrasos = [TimeSpan.FromMilliseconds(200)],
    };

    [Fact]
    public void Reagir_RegrasDeCadaServico()
    {
        var pedido = Guid.NewGuid();
        var estoque = new EstoqueCoreografado();
        var pagamento = new PagamentoCoreografado(limiteDeAprovacao: 1_000m);

        estoque.Reagir(Msg.PedidoCriado(pedido)).ShouldBeOfType<EstoqueReservado>().MessageId.ShouldBe($"{pedido:N}:EstoqueReservado");
        estoque.Reagir(Msg.PagamentoRecusado(pedido)).ShouldBeOfType<EstoqueLiberado>();
        estoque.Reagir(Msg.PagamentoAutorizado(pedido)).ShouldBeNull();

        Should.Throw<ErroTransitorioException>(() => pagamento.Reagir(Msg.EstoqueReservado(pedido)),
            "sem ter visto o PedidoCriado, o Pagamento não sabe o valor: fora de ordem, tente mais tarde");
        pagamento.Reagir(Msg.PedidoCriado(pedido, valor: 999m)).ShouldBeNull();
        pagamento.Reagir(Msg.EstoqueReservado(pedido)).ShouldBeOfType<PagamentoAutorizado>().AutorizacaoId.ShouldBe($"aut-{pedido:N}");

        var caro = Guid.NewGuid();
        pagamento.Reagir(Msg.PedidoCriado(caro, valor: 1_000.01m));
        pagamento.Reagir(Msg.EstoqueReservado(caro)).ShouldBeOfType<PagamentoRecusado>().Motivo.ShouldBe("limite excedido");
    }

    [Fact]
    public async Task FluxoCoreografado_PedidoAprovadoERecusado_EventosEmCadeiaSemCoordenador()
    {
        var exchange = Rabbit.NomeUnico("coreografia");
        await using var estoque = new EstoqueCoreografado();
        await using var pagamento = new PagamentoCoreografado(limiteDeAprovacao: 1_000m);
        await estoque.IniciarAsync(infra.Rabbit, exchange, Politica);
        await pagamento.IniciarAsync(infra.Rabbit, exchange, Politica);

        // O "espião": quem quiser a visão do fluxo inteiro precisa assinar tudo e correlacionar.
        var espiao = $"{exchange}.espiao";
        await using (var canal = await infra.Rabbit.CreateChannelAsync())
        {
            await canal.QueueDeclareAsync(espiao, durable: false, exclusive: false, autoDelete: false);
            await canal.QueueBindAsync(espiao, exchange, "#");
        }

        var aprovado = Guid.NewGuid();
        var recusado = Guid.NewGuid();
        await using (var publicacao = await CanalDePublicacao.CriarAsync(infra.Rabbit))
        {
            await ServicoCoreografado.PublicarEventoAsync(publicacao, exchange, Msg.PedidoCriado(aprovado, valor: 300m));
            await ServicoCoreografado.PublicarEventoAsync(publicacao, exchange, Msg.PedidoCriado(recusado, valor: 9_000m));
        }

        var vistos = new List<MensagemSaga>();
        await Esperar.Eventualmente(async () =>
        {
            vistos.AddRange((await Rabbit.DrenarAsync(infra.Rabbit, espiao)).Select(m => m.Ler<MensagemSaga>()));
            return vistos.Any(e => e is PagamentoAutorizado && e.PedidoId == aprovado)
                && vistos.Any(e => e is EstoqueLiberado && e.PedidoId == recusado);
        }, "os eventos dos dois pedidos aparecerem");

        vistos.Where(e => e.PedidoId == aprovado).Select(e => e.Tipo).ShouldBe(
            [nameof(PedidoCriado), nameof(EstoqueReservado), nameof(PagamentoAutorizado)]);
        vistos.Where(e => e.PedidoId == recusado).Select(e => e.Tipo).ShouldBe(
            [nameof(PedidoCriado), nameof(EstoqueReservado), nameof(PagamentoRecusado), nameof(EstoqueLiberado)]);
    }
}
