using Confluent.Kafka;
using F6M03.Kafka.Consumo;
using F6M03.Kafka.Contratos;
using F6M03.Kafka.Producao;
using F6M03.Kafka.Tests.Infra;
using F6M03.Kafka.Topicos;

namespace F6M03.Kafka.Tests;

/// <summary>Passos 2 e 3 — tópicos com partições, producer idempotente e ordenação por chave.</summary>
[Collection(ColecaoKafka.Nome)]
public sealed class TopicoEPublicacaoTests(KafkaFixture kafka)
{
    [Fact]
    public async Task CriarTopologia_TopicoComQuatroParticoes_CriaPrincipalRetryEDlq()
    {
        var topico = KafkaFixture.NomeUnico("pedidos");
        using var admin = new AdministradorDeTopicos(kafka.Bootstrap);

        await admin.CriarTopologiaAsync(topico, particoes: 4);
        await admin.CriarTopicoAsync(topico, particoes: 4); // de novo: não pode lançar

        admin.ContarParticoes(topico).ShouldBe(4);
        admin.ContarParticoes(AdministradorDeTopicos.NomeRetry(topico)).ShouldBe(4);
        admin.ContarParticoes(AdministradorDeTopicos.NomeDlq(topico)).ShouldBe(4);
    }

    [Fact]
    public void CriarConfigDoProducer_ProducaoSegura_IdempotenteAcksAllEMurmur2()
    {
        var config = PublicadorDePedidos.CriarConfig("localhost:9092");

        config.BootstrapServers.ShouldBe("localhost:9092");
        config.EnableIdempotence.ShouldBe(true);
        config.Acks.ShouldBe(Acks.All);
        config.Partitioner.ShouldBe(Partitioner.Murmur2Random);
        config.ClientId.ShouldBe("orderflow-pedidos");
    }

    [Fact]
    public async Task Publicar_PedidoCriado_FicaPersistidoComParticaoEOffset()
    {
        var topico = KafkaFixture.NomeUnico("pedidos");
        using (var admin = new AdministradorDeTopicos(kafka.Bootstrap)) await admin.CriarTopicoAsync(topico, 3);
        using var publicador = new PublicadorDePedidos(kafka.Bootstrap, topico);
        var evento = Apoio.NovoPedidoCriado();

        var entrega = await publicador.PublicarAsync(evento, TestContext.Current.CancellationToken);

        entrega.Status.ShouldBe(PersistenceStatus.Persisted);
        entrega.Topic.ShouldBe(topico);
        entrega.Partition.Value.ShouldBeInRange(0, 2);
        entrega.Offset.Value.ShouldBe(0);
        entrega.Message.Key.ShouldBe(evento.PedidoId.ToString());
    }

    [Fact]
    public async Task Publicar_MesmaChave_SempreNaMesmaParticaoComOffsetsCrescentes_ChavesDiferentesEspalham()
    {
        var topico = KafkaFixture.NomeUnico("pedidos");
        using (var admin = new AdministradorDeTopicos(kafka.Bootstrap)) await admin.CriarTopicoAsync(topico, 4);
        using var publicador = new PublicadorDePedidos(kafka.Bootstrap, topico);
        var ct = TestContext.Current.CancellationToken;
        var pedidoId = Guid.NewGuid();

        var mesmoPedido = new List<DeliveryResult<string, byte[]>>();
        mesmoPedido.Add(await publicador.PublicarAsync(Apoio.NovoPedidoCriado(pedidoId), ct));
        for (var i = 0; i < 4; i++)
            mesmoPedido.Add(await publicador.PublicarAsync(Apoio.NovoPedidoConfirmado(pedidoId), ct));

        mesmoPedido.Select(e => e.Partition.Value).Distinct().Count().ShouldBe(1, "mesma chave → mesma partição");
        mesmoPedido.Select(e => e.Offset.Value).ShouldBeInOrder(SortDirection.Ascending);

        var outros = await Task.WhenAll(Enumerable.Range(0, 30)
            .Select(_ => publicador.PublicarAsync(Apoio.NovoPedidoCriado(), ct)));
        outros.Select(e => e.Partition.Value).Distinct().Count().ShouldBeGreaterThan(1, "chaves diferentes se espalham pelas partições");
    }

    [Fact]
    public async Task Consumir_VariosPedidosEmParalelo_EventosDeCadaPedidoChegamNaOrdemPublicada()
    {
        var topico = KafkaFixture.NomeUnico("pedidos");
        using (var admin = new AdministradorDeTopicos(kafka.Bootstrap)) await admin.CriarTopicoAsync(topico, 4);
        var ct = TestContext.Current.CancellationToken;
        var pedidos = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();

        using (var publicador = new PublicadorDePedidos(kafka.Bootstrap, topico))
        {
            // Dispara tudo sem esperar uma a uma: o producer idempotente mantém a ordem por partição mesmo com retries.
            var envios = pedidos.Select(p => publicador.PublicarAsync(Apoio.NovoPedidoCriado(p), ct))
                .Concat(pedidos.Select(p => publicador.PublicarAsync(Apoio.NovoPedidoConfirmado(p), ct)))
                .ToList();
            await Task.WhenAll(envios);
        }

        var manipulador = new ManipuladorDeTeste();
        using var consumidor = new ConsumidorDePedidos(
            ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, KafkaFixture.NomeUnico("ordem")), [topico], manipulador);

        await Apoio.Eventualmente(async () =>
        {
            await consumidor.ProcessarProximoAsync(TimeSpan.FromMilliseconds(200), ct);
            return manipulador.Processados.Count == 20;
        }, "consumir os 20 eventos");

        foreach (var pedido in pedidos)
        {
            var doPedido = manipulador.Processados.Where(e => e.PedidoId == pedido).ToList();
            doPedido.Count.ShouldBe(2);
            doPedido[0].ShouldBeOfType<PedidoCriado>("Criado sempre antes de Confirmado para o mesmo pedido");
            doPedido[1].ShouldBeOfType<PedidoConfirmado>();
        }
    }
}
