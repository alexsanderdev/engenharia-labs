using System.Text;
using Confluent.Kafka;
using F6M03.Kafka.Consumo;
using F6M03.Kafka.Contratos;
using F6M03.Kafka.Producao;
using F6M03.Kafka.Tests.Infra;
using F6M03.Kafka.Topicos;

namespace F6M03.Kafka.Tests;

/// <summary>Passo 6 — consumidor idempotente (sem broker e com broker).</summary>
public sealed class ManipuladorIdempotenteTests
{
    [Fact]
    public async Task MesmoEventoEntregueDuasVezes_EfeitoAconteceUmaVez()
    {
        var interno = new ManipuladorDeTeste();
        var idempotente = new ManipuladorIdempotente(interno, new RegistroEmMemoria());
        var evento = Apoio.NovoPedidoCriado();

        await idempotente.ManipularAsync(evento, TestContext.Current.CancellationToken);
        await idempotente.ManipularAsync(evento, TestContext.Current.CancellationToken);

        interno.Processados.Count.ShouldBe(1);
    }

    [Fact]
    public async Task FalhaNoProcessamento_LiberaOEvento_ProximaEntregaProcessa()
    {
        var interno = new ManipuladorDeTeste();
        var registro = new RegistroEmMemoria();
        var idempotente = new ManipuladorIdempotente(interno, registro);
        var evento = Apoio.NovoPedidoCriado();
        interno.FalharPara(evento.PedidoId, vezes: 1);

        await Should.ThrowAsync<TimeoutException>(() => idempotente.ManipularAsync(evento, TestContext.Current.CancellationToken));
        registro.Quantidade.ShouldBe(0, "falhou: o evento não pode ficar marcado como processado");

        await idempotente.ManipularAsync(evento, TestContext.Current.CancellationToken);
        interno.Processados.ShouldBe([evento]);
    }
}

/// <summary>Passos 6 e 7 — idempotência ponta a ponta e retry/DLQ feitos pelo consumidor.</summary>
[Collection(ColecaoKafka.Nome)]
public sealed class RetryEDlqTests(KafkaFixture kafka)
{
    private static readonly TimeSpan Espera = TimeSpan.FromMilliseconds(200);

    [Fact]
    public async Task ProdutorReenviaOMesmoEvento_ConsumidorIdempotenteProcessaUmaVez()
    {
        var ct = TestContext.Current.CancellationToken;
        var topico = await CriarTopologiaAsync();
        var evento = Apoio.NovoPedidoCriado();
        using (var publicador = new PublicadorDePedidos(kafka.Bootstrap, topico))
        {
            // A API deu timeout e o cliente repetiu: são DUAS mensagens no log (EnableIdempotence não evita isso).
            await publicador.PublicarAsync(evento, ct);
            await publicador.PublicarAsync(evento, ct);
        }

        var efeito = new ManipuladorDeTeste();
        var registro = new RegistroEmMemoria();
        using var consumidor = new ConsumidorDePedidos(
            ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, KafkaFixture.NomeUnico("notificacao")), [topico],
            new ManipuladorIdempotente(efeito, registro));

        var lidas = 0;
        await Apoio.Eventualmente(async () =>
        {
            if (await consumidor.ProcessarProximoAsync(Espera, ct)) lidas++;
            return lidas == 2;
        }, "ler as duas cópias");

        efeito.Processados.Count.ShouldBe(1, "um e-mail, não dois");
    }

    [Fact]
    public async Task FalhaTransitoria_VaiParaORetryEDepoisEhProcessada_SemDlq()
    {
        var ct = TestContext.Current.CancellationToken;
        var topico = await CriarTopologiaAsync();
        var evento = Apoio.NovoPedidoCriado();
        await PublicarAsync(topico, evento);

        var manipulador = new ManipuladorDeTeste();
        manipulador.FalharPara(evento.PedidoId, vezes: 1);
        using var encaminhador = new EncaminhadorDeFalhas(kafka.Bootstrap, topico, maxTentativas: 3);
        using var consumidor = new ConsumidorDePedidos(
            ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, KafkaFixture.NomeUnico("notificacao")),
            [topico, AdministradorDeTopicos.NomeRetry(topico)], manipulador, encaminhador);

        await Apoio.Eventualmente(async () =>
        {
            await consumidor.ProcessarProximoAsync(Espera, ct);
            return manipulador.Processados.Count == 1;
        }, "processar o evento depois de um retry");

        manipulador.TentativasDo(evento.PedidoId).ShouldBe(2);
        var retry = Apoio.LerDoInicio(kafka.Bootstrap, AdministradorDeTopicos.NomeRetry(topico), 1);
        retry.Count.ShouldBe(1);
        Cabecalhos.Ler(retry[0].Message.Headers, Cabecalhos.Tentativas).ShouldBe("1");
        Apoio.LerDoInicio(kafka.Bootstrap, AdministradorDeTopicos.NomeDlq(topico), 1, TimeSpan.FromSeconds(1)).ShouldBeEmpty();
    }

    [Fact]
    public async Task FalhaPermanente_EsgotaAsTentativas_VaiParaDlqComHeadersDeDiagnostico()
    {
        var ct = TestContext.Current.CancellationToken;
        var topico = await CriarTopologiaAsync();
        var evento = Apoio.NovoPedidoCriado();
        await PublicarAsync(topico, evento);

        var manipulador = new ManipuladorDeTeste();
        manipulador.FalharPara(evento.PedidoId, int.MaxValue);
        using var encaminhador = new EncaminhadorDeFalhas(kafka.Bootstrap, topico, maxTentativas: 3);
        using var consumidor = new ConsumidorDePedidos(
            ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, KafkaFixture.NomeUnico("notificacao")),
            [topico, AdministradorDeTopicos.NomeRetry(topico)], manipulador, encaminhador);

        await Apoio.Eventualmente(async () =>
        {
            await consumidor.ProcessarProximoAsync(Espera, ct);
            return manipulador.TentativasDo(evento.PedidoId) == 3;
        }, "tentar 3 vezes");

        var dlq = Apoio.LerDoInicio(kafka.Bootstrap, AdministradorDeTopicos.NomeDlq(topico), 1);
        dlq.Count.ShouldBe(1);
        var morta = dlq[0].Message;
        morta.Key.ShouldBe(evento.PedidoId.ToString());
        Cabecalhos.Ler(morta.Headers, Cabecalhos.Tentativas).ShouldBe("3");
        Cabecalhos.Ler(morta.Headers, Cabecalhos.ErroTipo).ShouldBe(typeof(TimeoutException).FullName);
        Cabecalhos.Ler(morta.Headers, Cabecalhos.ErroMensagem)!.ShouldContain("e-mail fora do ar");
        Cabecalhos.Ler(morta.Headers, Cabecalhos.TopicoOriginal).ShouldBe(topico);
        Cabecalhos.Ler(morta.Headers, Cabecalhos.ParticaoOriginal).ShouldNotBeNull();
        Cabecalhos.Ler(morta.Headers, Cabecalhos.OffsetOriginal).ShouldNotBeNull();
        Cabecalhos.Ler(morta.Headers, Cabecalhos.EventoId).ShouldBe(evento.EventoId.ToString());
        morta.Headers.Count(h => h.Key == Cabecalhos.Tentativas).ShouldBe(1, "não acumule headers x- antigos");
        SerializadorDeEventos.Ler(morta).ShouldBe(evento, "o corpo vai intacto para a DLQ (reprocessável)");

        Apoio.LerDoInicio(kafka.Bootstrap, AdministradorDeTopicos.NomeRetry(topico), 2).Count.ShouldBe(2);
    }

    [Fact]
    public async Task ContratoNaoSuportado_VaiDiretoParaDlqSemRetryESemChamarONegocio()
    {
        var ct = TestContext.Current.CancellationToken;
        var topico = await CriarTopologiaAsync();
        var headers = new Headers();
        Cabecalhos.Escrever(headers, Cabecalhos.TipoEvento, "PedidoCriado");
        Cabecalhos.Escrever(headers, Cabecalhos.VersaoContrato, "2");
        using (var cru = new ProducerBuilder<string, byte[]>(new ProducerConfig { BootstrapServers = kafka.Bootstrap }).Build())
        {
            await cru.ProduceAsync(topico, new Message<string, byte[]>
            {
                Key = "ped-v2", Value = Encoding.UTF8.GetBytes("""{"pedidoId":"x","novoCampo":1}"""), Headers = headers,
            }, ct);
        }

        var manipulador = new ManipuladorDeTeste();
        using var encaminhador = new EncaminhadorDeFalhas(kafka.Bootstrap, topico, maxTentativas: 3);
        using var consumidor = new ConsumidorDePedidos(
            ConsumidorDePedidos.CriarConfig(kafka.Bootstrap, KafkaFixture.NomeUnico("notificacao")),
            [topico, AdministradorDeTopicos.NomeRetry(topico)], manipulador, encaminhador);

        await Apoio.Eventualmente(async () => await consumidor.ProcessarProximoAsync(Espera, ct), "ler a mensagem v2");

        var dlq = Apoio.LerDoInicio(kafka.Bootstrap, AdministradorDeTopicos.NomeDlq(topico), 1);
        dlq.Count.ShouldBe(1);
        Cabecalhos.Ler(dlq[0].Message.Headers, Cabecalhos.Tentativas).ShouldBe("1");
        Cabecalhos.Ler(dlq[0].Message.Headers, Cabecalhos.ErroTipo).ShouldBe(typeof(ContratoNaoSuportadoException).FullName);
        Cabecalhos.Ler(dlq[0].Message.Headers, Cabecalhos.VersaoContrato).ShouldBe("2", "headers originais são preservados");
        manipulador.Processados.ShouldBeEmpty();
        Apoio.LerDoInicio(kafka.Bootstrap, AdministradorDeTopicos.NomeRetry(topico), 1, TimeSpan.FromSeconds(1)).ShouldBeEmpty();
    }

    private async Task<string> CriarTopologiaAsync()
    {
        var topico = KafkaFixture.NomeUnico("pedidos");
        using var admin = new AdministradorDeTopicos(kafka.Bootstrap);
        await admin.CriarTopologiaAsync(topico, particoes: 2);
        return topico;
    }

    private async Task PublicarAsync(string topico, IEventoDePedido evento)
    {
        using var publicador = new PublicadorDePedidos(kafka.Bootstrap, topico);
        await publicador.PublicarAsync(evento, TestContext.Current.CancellationToken);
    }
}
