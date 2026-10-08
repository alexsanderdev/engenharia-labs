using F6M01.Mensageria.Contratos;
using F6M01.Mensageria.Publicacao;
using F6M01.Mensageria.Tests.Infra;
using F6M01.Mensageria.Topologia;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace F6M01.Mensageria.Tests;

/// <summary>Passo 5 — publicação: comando para UM destinatário, evento para N assinantes (com broker).</summary>
public class ComandosEEventosTests(RabbitFixture rabbit) : RabbitTestBase(rabbit)
{
    private static readonly ReservarEstoque Comando = new(Guid.NewGuid(), [new ItemDaReserva("SKU-CAFE", 2)]);

    [Fact]
    public async Task EnviarComando_ChegaSoNaFilaDoEstoque_PersistenteComEnvelopeCompleto()
    {
        await DeclararTopologiaAsync();
        await using var publicador = await PublicadorDeMensagens.CriarAsync(Conexao);

        var enviado = await publicador.EnviarAsync(Comando, correlationId: "pedido-123");

        var recebido = await LerUmaAsync(TopologiaOrderFlow.FilaReservarEstoque);
        recebido.BasicProperties.Persistent.ShouldBeTrue();
        recebido.BasicProperties.MessageId.ShouldBe(enviado.MessageId.ToString("D"));
        recebido.BasicProperties.CorrelationId.ShouldBe("pedido-123");
        recebido.BasicProperties.Type.ShouldBe("orderflow.estoque.reservar-estoque");

        var envelope = MapeamentoAmqp.ParaEnvelope(recebido.BasicProperties, recebido.Body);
        envelope.LerCorpo<ReservarEstoque>().Itens.ShouldHaveSingleItem().Sku.ShouldBe("SKU-CAFE");

        (await Rabbit.ProntasNaFilaAsync(TopologiaOrderFlow.FilaNotificacaoPedidoCriado)).ShouldBe(0u);
        (await Rabbit.ProntasNaFilaAsync(TopologiaOrderFlow.FilaFidelidadeEventosDePedido)).ShouldBe(0u);
    }

    [Fact]
    public async Task EnviarComando_SemFilaDeDestino_LancaEmVezDePerderAMensagem()
    {
        await DeclararTopologiaAsync();
        await using (var canal = await Conexao.CreateChannelAsync())
            await canal.QueueDeleteAsync(TopologiaOrderFlow.FilaReservarEstoque);
        await using var publicador = await PublicadorDeMensagens.CriarAsync(Conexao);

        // mandatory: true + publisher confirms → o broker devolve (basic.return) e o await lança.
        await Should.ThrowAsync<PublishReturnException>(() => publicador.EnviarAsync(Comando));
    }

    [Fact]
    public async Task PublicarPedidoCriado_CadaAssinanteRecebeASuaCopia()
    {
        await DeclararTopologiaAsync();
        await using var publicador = await PublicadorDeMensagens.CriarAsync(Conexao);
        var evento = new PedidoCriado(Guid.NewGuid(), Guid.NewGuid(), 120m, DateTimeOffset.UtcNow);

        var publicado = await publicador.PublicarAsync(evento);

        var naNotificacao = await LerUmaAsync(TopologiaOrderFlow.FilaNotificacaoPedidoCriado);
        var naFidelidade = await LerUmaAsync(TopologiaOrderFlow.FilaFidelidadeEventosDePedido);
        naNotificacao.BasicProperties.MessageId.ShouldBe(publicado.MessageId.ToString("D"));
        naFidelidade.BasicProperties.MessageId.ShouldBe(publicado.MessageId.ToString("D"), "a MESMA mensagem, uma cópia por fila");
        naNotificacao.RoutingKey.ShouldBe("pedido.criado");
        (await Rabbit.ProntasNaFilaAsync(TopologiaOrderFlow.FilaReservarEstoque)).ShouldBe(0u, "evento não é comando");
    }

    [Fact]
    public async Task PublicarPedidoCancelado_SoQuemAssinouPedidoCuringaRecebe()
    {
        await DeclararTopologiaAsync();
        await using var publicador = await PublicadorDeMensagens.CriarAsync(Conexao);

        await publicador.PublicarAsync(new PedidoCancelado(Guid.NewGuid(), "pagamento recusado"));

        var naFidelidade = await LerUmaAsync(TopologiaOrderFlow.FilaFidelidadeEventosDePedido);
        naFidelidade.RoutingKey.ShouldBe("pedido.cancelado");
        (await Rabbit.ProntasNaFilaAsync(TopologiaOrderFlow.FilaNotificacaoPedidoCriado)).ShouldBe(0u,
            "a Notificação assinou só 'pedido.criado'");
    }

    [Fact]
    public async Task TopicCuringa_AsteriscoCasaExatamenteUmaPalavra_CerquilhaCasaZeroOuMais()
    {
        await DeclararTopologiaAsync();
        await using var canal = await Conexao.CreateChannelAsync();
        await canal.QueueDeclareAsync(RabbitFixture.FilaEspia, durable: false, exclusive: false, autoDelete: false);
        await canal.QueueBindAsync(RabbitFixture.FilaEspia, TopologiaOrderFlow.ExchangeEventos, "pedido.#");

        // Um evento com routing key de 3 palavras (ex.: um futuro "pedido.item.adicionado").
        await canal.BasicPublishAsync(TopologiaOrderFlow.ExchangeEventos, "pedido.item.adicionado", "{}"u8.ToArray());

        (await LerUmaAsync(RabbitFixture.FilaEspia)).RoutingKey.ShouldBe("pedido.item.adicionado");
        (await Rabbit.ProntasNaFilaAsync(TopologiaOrderFlow.FilaFidelidadeEventosDePedido)).ShouldBe(0u,
            "'pedido.*' casa 'pedido.criado', mas não 'pedido.item.adicionado'");
    }

    [Fact]
    public async Task PublicarEvento_SemNenhumAssinante_NaoEhErro()
    {
        await DeclararTopologiaAsync();
        await using (var canal = await Conexao.CreateChannelAsync())
        {
            await canal.QueueDeleteAsync(TopologiaOrderFlow.FilaNotificacaoPedidoCriado);
            await canal.QueueDeleteAsync(TopologiaOrderFlow.FilaFidelidadeEventosDePedido);
        }

        await using var publicador = await PublicadorDeMensagens.CriarAsync(Conexao);

        await Should.NotThrowAsync(() =>
            publicador.PublicarAsync(new PedidoCriado(Guid.NewGuid(), Guid.NewGuid(), 10m, DateTimeOffset.UtcNow)));
    }
}
