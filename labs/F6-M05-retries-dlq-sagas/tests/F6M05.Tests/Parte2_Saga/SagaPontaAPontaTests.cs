using F6M05.Sagas.Mensageria;
using F6M05.Sagas.Retry;
using F6M05.Sagas.Saga;
using F6M05.Tests.Infra;

namespace F6M05.Tests.Parte2_Saga;

/// <summary>
/// Passo 9 — tudo junto: a saga hospedada no RabbitMQ (com o seu consumidor com retry/DLQ),
/// estado no SQL Server e participantes falsos respondendo por AMQP.
/// </summary>
public sealed class SagaPontaAPontaTests(InfraFixture infra) : SqlTestBase(infra)
{
    private static readonly PoliticaDeRetry Politica = new()
    {
        RetentativasImediatas = 1,
        Atrasos = [TimeSpan.FromMilliseconds(200)],
    };

    private const decimal LimiteDoPagamento = 1_000m;

    private readonly NomesDaSaga _nomes = new(Rabbit.NomeUnico("orderflow"));
    private RepositorioDeSagasSql Repositorio => new(Infra.Sql);

    private Task PublicarNaSagaAsync(MensagemSaga evento) =>
        Rabbit.PublicarAsync(Infra.Rabbit, _nomes.FilaDeEventos,
            System.Text.Encoding.UTF8.GetString(Serializador.Serializar(evento)), evento.MessageId, evento.Tipo);

    private Task<HospedeiroDaSaga> HospedarAsync() =>
        HospedeiroDaSaga.IniciarAsync(Infra.Rabbit, _nomes, Repositorio, TimeProvider.System, Politica);

    private async Task<bool> StatusEhAsync(Guid pedido, StatusSaga status) =>
        (await Repositorio.ObterAsync(pedido))?.Status == status;

    [Fact]
    public async Task PedidoAprovadoEPedidoRecusado_TerminamConcluidoECanceladoComCompensacao()
    {
        await using var participantes = await ParticipantesFalsos.IniciarAsync(Infra.Rabbit, _nomes, LimiteDoPagamento);
        await using var saga = await HospedarAsync();
        var aprovado = Guid.NewGuid();
        var recusado = Guid.NewGuid();

        await PublicarNaSagaAsync(Msg.PedidoCriado(aprovado, valor: 300m));
        await PublicarNaSagaAsync(Msg.PedidoCriado(recusado, valor: 5_000m));

        await Esperar.Eventualmente(() => StatusEhAsync(aprovado, StatusSaga.Concluida), "o pedido aprovado concluir");
        await Esperar.Eventualmente(() => StatusEhAsync(recusado, StatusSaga.Cancelada), "o pedido recusado ser cancelado");
        await Esperar.Eventualmente(() => participantes.DoPedido(recusado).OfType<CancelarPedido>().Any(), "o módulo de Pedidos receber o cancelamento");

        participantes.DoPedido(aprovado).Select(c => c.Tipo).ShouldBe(
            [nameof(ReservarEstoque), nameof(AutorizarPagamento), nameof(ConfirmarPedido)]);
        participantes.DoPedido(recusado).Select(c => c.Tipo).ShouldBe(
            [nameof(ReservarEstoque), nameof(AutorizarPagamento), nameof(LiberarEstoque), nameof(CancelarPedido)]);
        participantes.DoPedido(recusado).OfType<CancelarPedido>().Single().Motivo.ShouldContain("limite excedido");
    }

    [Fact]
    public async Task MensagemInvalidaEDuplicada_NaoTravamASaga()
    {
        await using var participantes = await ParticipantesFalsos.IniciarAsync(Infra.Rabbit, _nomes, LimiteDoPagamento);
        await using var saga = await HospedarAsync();
        var pedido = Guid.NewGuid();
        var criado = Msg.PedidoCriado(pedido, valor: 100m);

        await Rabbit.PublicarAsync(Infra.Rabbit, _nomes.FilaDeEventos, "{ \"$tipo\": \"PedidoCriado\", lixo", "m-lixo", "PedidoCriado");
        await PublicarNaSagaAsync(criado);
        await PublicarNaSagaAsync(criado); // o produtor reenviou (at-least-once)

        await Esperar.Eventualmente(() => StatusEhAsync(pedido, StatusSaga.Concluida), "o pedido concluir apesar do lixo e da duplicata");
        var dlq = PoliticaDeRetry.NomeDaDlq(_nomes.FilaDeEventos);
        await Esperar.Eventualmente(async () => await Rabbit.ContarAsync(Infra.Rabbit, dlq) == 1, "o lixo parar na DLQ da saga");

        var estado = (await Repositorio.ObterAsync(pedido)).ShouldNotBeNull();
        estado.Versao.ShouldBe(3, "PedidoCriado, EstoqueReservado e PagamentoAutorizado: a duplicata não gravou nada");
        participantes.DoPedido(pedido).OfType<ConfirmarPedido>().Select(c => c.MessageId).Distinct().ShouldHaveSingleItem();
    }
}
