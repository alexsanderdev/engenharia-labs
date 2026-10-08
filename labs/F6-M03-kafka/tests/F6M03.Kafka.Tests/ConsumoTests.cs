using Confluent.Kafka;
using F6M03.Kafka.Consumo;
using F6M03.Kafka.Producao;
using F6M03.Kafka.Tests.Infra;
using F6M03.Kafka.Topicos;

namespace F6M03.Kafka.Tests;

/// <summary>Passos 4 e 5 — commit manual (at-least-once), AutoOffsetReset, consumer groups e rebalance.</summary>
[Collection(ColecaoKafka.Nome)]
public sealed class ConsumoTests(KafkaFixture kafka)
{
    private static readonly TimeSpan Espera = TimeSpan.FromMilliseconds(200);

    [Fact]
    public void CriarConfigDoConsumidor_AtLeastOnce_SemAutoCommitEComInicioEscolhido()
    {
        var config = ConsumidorDePedidos.CriarConfig("localhost:9092", "notificacao", AutoOffsetReset.Latest);

        config.GroupId.ShouldBe("notificacao");
        config.EnableAutoCommit.ShouldBe(false);
        config.EnableAutoOffsetStore.ShouldBe(false);
        config.AutoOffsetReset.ShouldBe(AutoOffsetReset.Latest);
        ConsumidorDePedidos.CriarConfig("localhost:9092", "g").AutoOffsetReset.ShouldBe(AutoOffsetReset.Earliest);
    }

    [Fact]
    public async Task QuedaAntesDoCommit_MensagemEhEntregueDeNovo_MasAJaCommitadaNao()
    {
        var ct = TestContext.Current.CancellationToken;
        var topico = await CriarTopicoAsync(particoes: 1);
        var e1 = Apoio.NovoPedidoCriado();
        var e2 = Apoio.NovoPedidoCriado();
        await PublicarAsync(topico, e1, e2);
        var grupo = KafkaFixture.NomeUnico("notificacao");

        // Instância 1: processa e1 (commit) e "cai" no meio de e2 (exceção, sem commit).
        var instancia1 = new ManipuladorDeTeste();
        instancia1.FalharPara(e2.PedidoId, int.MaxValue);
        using (var consumidor1 = new ConsumidorDePedidos(ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, grupo), [topico], instancia1))
        {
            var caiu = false;
            await Apoio.Eventualmente(async () =>
            {
                try { await consumidor1.ProcessarProximoAsync(Espera, ct); }
                catch (TimeoutException) { caiu = true; }
                return caiu;
            }, "a instância 1 processar e1 e falhar em e2");
            instancia1.Processados.ShouldBe([e1]);
        } // processo encerrado sem commit de e2

        // Instância 2 (mesmo grupo): continua do último offset COMMITADO.
        var instancia2 = new ManipuladorDeTeste();
        using var consumidor2 = new ConsumidorDePedidos(ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, grupo), [topico], instancia2);
        await Apoio.Eventualmente(async () => await consumidor2.ProcessarProximoAsync(Espera, ct), "a instância 2 receber uma mensagem");

        instancia2.Processados.ShouldBe([e2], "e2 é reentregue (at-least-once) e e1, já commitado, não");
    }

    [Fact]
    public async Task AutoOffsetReset_Earliest_GrupoNovoLeTodoOHistorico()
    {
        var ct = TestContext.Current.CancellationToken;
        var topico = await CriarTopicoAsync(particoes: 2);
        await PublicarAsync(topico, Apoio.NovoPedidoCriado(), Apoio.NovoPedidoCriado(), Apoio.NovoPedidoCriado());

        var manipulador = new ManipuladorDeTeste();
        using var consumidor = new ConsumidorDePedidos(
            ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, KafkaFixture.NomeUnico("relatorio"), AutoOffsetReset.Earliest), [topico], manipulador);

        await Apoio.Eventualmente(async () =>
        {
            await consumidor.ProcessarProximoAsync(Espera, ct);
            return manipulador.Processados.Count == 3;
        }, "ler as 3 mensagens antigas");
    }

    [Fact]
    public async Task AutoOffsetReset_Latest_GrupoNovoIgnoraHistoricoELeSoOQueChegaDepois()
    {
        var ct = TestContext.Current.CancellationToken;
        var topico = await CriarTopicoAsync(particoes: 1);
        var antigos = new[] { Apoio.NovoPedidoCriado(), Apoio.NovoPedidoCriado(), Apoio.NovoPedidoCriado() };
        await PublicarAsync(topico, antigos);

        var manipulador = new ManipuladorDeTeste();
        using var consumidor = new ConsumidorDePedidos(
            ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, KafkaFixture.NomeUnico("auditoria"), AutoOffsetReset.Latest), [topico], manipulador);
        using var publicador = new PublicadorDePedidos(kafka.Bootstrap, topico);

        // Publica um evento novo por volta até o consumidor (que entra no grupo em algum momento) receber algo.
        await Apoio.Eventualmente(async () =>
        {
            await publicador.PublicarAsync(Apoio.NovoPedidoCriado(), ct);
            await consumidor.ProcessarProximoAsync(Espera, ct);
            return manipulador.Processados.Count > 0;
        }, "receber um evento novo");

        manipulador.Processados.ShouldNotContain(e => antigos.Any(a => a.EventoId == e.EventoId));
    }

    [Fact]
    public async Task ConsumerGroup_DoisConsumidoresNoMesmoGrupo_DividemAsParticoesECadaEventoEhProcessadoUmaVez()
    {
        var ct = TestContext.Current.CancellationToken;
        var topico = await CriarTopicoAsync(particoes: 4);
        var grupo = KafkaFixture.NomeUnico("estoque");
        var manipulador = new ManipuladorDeTeste();
        using var c1 = new ConsumidorDePedidos(ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, grupo), [topico], manipulador);
        using var c2 = new ConsumidorDePedidos(ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, grupo), [topico], manipulador);

        await Apoio.GirarAte(
            () => c1.ParticoesAtribuidas.Count == 2 && c2.ParticoesAtribuidas.Count == 2,
            "o grupo dividir 4 partições em 2 + 2", c1, c2);
        c1.ParticoesAtribuidas.Intersect(c2.ParticoesAtribuidas).ShouldBeEmpty();

        var eventos = Enumerable.Range(0, 20).Select(_ => Apoio.NovoPedidoCriado()).ToArray();
        await PublicarAsync(topico, eventos);
        await Apoio.Eventualmente(async () =>
        {
            await c1.ProcessarProximoAsync(TimeSpan.FromMilliseconds(50), ct);
            await c2.ProcessarProximoAsync(TimeSpan.FromMilliseconds(50), ct);
            return manipulador.Processados.Count >= 20;
        }, "o grupo processar os 20 eventos");

        manipulador.Processados.Select(e => e.EventoId).ShouldBe(eventos.Select(e => e.EventoId), ignoreOrder: true);
    }

    [Fact]
    public async Task Rebalance_QuandoUmConsumidorSaiDoGrupo_OOutroAssumeTodasAsParticoes()
    {
        var topico = await CriarTopicoAsync(particoes: 4);
        var grupo = KafkaFixture.NomeUnico("fidelidade");
        var manipulador = new ManipuladorDeTeste();
        using var c1 = new ConsumidorDePedidos(ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, grupo), [topico], manipulador);
        var c2 = new ConsumidorDePedidos(ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, grupo), [topico], manipulador);

        await Apoio.GirarAte(
            () => c1.ParticoesAtribuidas.Count == 2 && c2.ParticoesAtribuidas.Count == 2,
            "o grupo dividir 4 partições em 2 + 2", c1, c2);

        c2.Dispose(); // a instância 2 sai do grupo (deploy, scale-in)

        await Apoio.GirarAte(() => c1.ParticoesAtribuidas.Count == 4, "c1 assumir as 4 partições", c1);
        c1.ParticoesAtribuidas.ShouldBe([0, 1, 2, 3]);

        var historico = c1.HistoricoDeRebalance;
        historico.ShouldContain(e => e.Tipo == "revogadas", "o rebalance tira partições antes de redistribuir");
        historico.Last(e => e.Tipo == "atribuidas").Particoes.Count.ShouldBe(4);
    }

    [Fact]
    public async Task GruposDiferentes_CadaGrupoRecebeTodosOsEventos()
    {
        var ct = TestContext.Current.CancellationToken;
        var topico = await CriarTopicoAsync(particoes: 2);
        var eventos = Enumerable.Range(0, 5).Select(_ => Apoio.NovoPedidoCriado()).ToArray();
        await PublicarAsync(topico, eventos);

        var notificacao = new ManipuladorDeTeste();
        var estoque = new ManipuladorDeTeste();
        using var cNotificacao = new ConsumidorDePedidos(ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, KafkaFixture.NomeUnico("notificacao")), [topico], notificacao);
        using var cEstoque = new ConsumidorDePedidos(ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, KafkaFixture.NomeUnico("estoque")), [topico], estoque);

        await Apoio.Eventualmente(async () =>
        {
            await cNotificacao.ProcessarProximoAsync(TimeSpan.FromMilliseconds(50), ct);
            await cEstoque.ProcessarProximoAsync(TimeSpan.FromMilliseconds(50), ct);
            return notificacao.Processados.Count == 5 && estoque.Processados.Count == 5;
        }, "os dois grupos lerem os 5 eventos");
    }

    private async Task<string> CriarTopicoAsync(int particoes)
    {
        var topico = KafkaFixture.NomeUnico("pedidos");
        using var admin = new AdministradorDeTopicos(kafka.Bootstrap);
        await admin.CriarTopicoAsync(topico, particoes);
        return topico;
    }

    private async Task PublicarAsync(string topico, params Contratos.IEventoDePedido[] eventos)
    {
        using var publicador = new PublicadorDePedidos(kafka.Bootstrap, topico);
        foreach (var e in eventos) await publicador.PublicarAsync(e, TestContext.Current.CancellationToken);
    }
}
