using F6M04.Pedidos.Dominio;
using F6M04.Pedidos.Infra;
using F6M04.Pedidos.Mensageria;
using F6M04.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace F6M04.Tests;

/// <summary>Passos 3 a 6 — o OutboxProcessor: publicar, falhar, desistir, ordenar, concorrer, cair e limpar.</summary>
[Collection(ColecaoInfra.Nome)]
public sealed class OutboxProcessorTests(InfraFixture infra) : TesteComInfra(infra)
{
    [Fact]
    public async Task ProcessarLote_PublicaAsPendentesEmOrdemEMarcaComoProcessadas()
    {
        await CriarPedidoAsync("PED-0001");
        await CriarPedidoAsync("PED-0002");
        await CriarPedidoAsync("PED-0003");
        var processor = CriarProcessor(Publicador);

        (await processor.ProcessarLoteAsync()).ShouldBe(3);

        var outbox = await LerOutboxAsync();
        Publicador.Publicadas.Select(m => m.MessageId).ShouldBe(outbox.Select(m => m.Id), "na ordem de gravação, com o Id da Outbox como MessageId");
        Publicador.Publicadas.Select(m => m.Payload).ShouldAllBe(p => p.Contains("PED-000"));
        outbox.ShouldAllBe(m => m.ProcessadoEm == Relogio.GetUtcNow() && m.Tentativas == 1 && m.UltimoErro == null);

        (await processor.ProcessarLoteAsync()).ShouldBe(0, "o que já foi publicado não sai de novo");
        Publicador.Publicadas.Count.ShouldBe(3);
    }

    [Fact]
    public async Task BrokerForaDoArNoCommit_PedidoFicaSalvoEOEventoSaiQuandoOBrokerVolta()
    {
        // Um RabbitMQ "de verdade" fora do ar: a porta não tem ninguém escutando (connection refused).
        var portaMorta = BrokerDeTeste.PortaSemNinguem();
        await using var foraDoAr = new PublicadorRabbitMq(Options.Create(new RabbitMqOptions
        {
            ConnectionString = $"amqp://guest:guest@127.0.0.1:{portaMorta}/",
            Exchange = Topologia.Exchange,
            TempoMaximoDeConexao = TimeSpan.FromSeconds(3),
        }));
        var servicosSemBroker = CriarServicos(ajustar: s => s.AddSingleton<IPublicadorDeMensagens>(foraDoAr));

        // 1) O caso de uso não depende do broker: o pedido é criado.
        await CriarPedidoAsync("PED-0001", servicos: servicosSemBroker);
        (await ContarPedidosAsync()).ShouldBe(1);

        // 2) O processor tenta, falha, registra a tentativa e o erro e agenda a próxima (backoff de 10 s).
        (await CriarProcessor(foraDoAr).ProcessarLoteAsync()).ShouldBe(0);
        var pendente = (await LerOutboxAsync()).ShouldHaveSingleItem();
        pendente.ProcessadoEm.ShouldBeNull();
        pendente.Tentativas.ShouldBe(1);
        pendente.UltimoErro.ShouldNotBeNullOrWhiteSpace();
        pendente.ProximaTentativaEm.ShouldBe(Relogio.GetUtcNow() + TimeSpan.FromSeconds(10));

        // 3) O broker volta, mas o backoff ainda não venceu: nada sai.
        await using var rabbit = new PublicadorRabbitMq(Options.Create(new RabbitMqOptions { ConnectionString = Infra.AmqpUri, Exchange = Topologia.Exchange }));
        var processor = CriarProcessor(rabbit);
        (await processor.ProcessarLoteAsync()).ShouldBe(0);

        // 4) Venceu: o evento sai — com o MESMO MessageId gravado no commit.
        Relogio.Advance(TimeSpan.FromSeconds(10));
        (await processor.ProcessarLoteAsync()).ShouldBe(1);

        (await Broker.LerAsync(Topologia.Fila, 1)).ShouldHaveSingleItem().MessageId.ShouldBe(pendente.Id.ToString());
        var publicada = (await LerOutboxAsync()).ShouldHaveSingleItem();
        publicada.ProcessadoEm.ShouldBe(Relogio.GetUtcNow());
        publicada.Tentativas.ShouldBe(2);
        publicada.UltimoErro.ShouldBeNull();
    }

    [Fact]
    public async Task ProcessarLote_MensagemEnvenenada_ParaDeSerTentadaDepoisDeNTentativas()
    {
        var ruim = await CriarPedidoAsync("PED-RUIM");
        await CriarPedidoAsync("PED-BOM");
        var publicador = new PublicadorQueGrava((m, _) =>
            m.ChaveDeOrdenacao == ruim.ToString() ? new FalhaSimuladaException("payload recusado pelo broker") : null);
        var processor = CriarProcessor(publicador); // MaximoDeTentativas = 3

        for (var rodada = 0; rodada < 6; rodada++)
        {
            await processor.ProcessarLoteAsync();
            Relogio.Advance(TimeSpan.FromMinutes(1)); // vence qualquer backoff
        }

        var ruimId = (await LerOutboxAsync()).Single(m => m.ChaveDeOrdenacao == ruim.ToString());
        publicador.TentativasDe(ruimId.Id).ShouldBe(3, "depois de 3 tentativas a mensagem não é mais oferecida ao broker");
        ruimId.Tentativas.ShouldBe(3);
        ruimId.ProcessadoEm.ShouldBeNull();
        ruimId.UltimoErro.ShouldNotBeNull().ShouldContain("payload recusado");

        publicador.Publicadas.ShouldHaveSingleItem().Payload.ShouldContain("PED-BOM", Case.Sensitive, "a mensagem envenenada não trava os OUTROS pedidos");
    }

    [Fact]
    public async Task ProcessarLote_FalhaNumEvento_SeguraOsEventosPosterioresDoMesmoPedido()
    {
        var x = await CriarPedidoAsync("PED-X");
        await ConfirmarPedidoAsync(x);
        await CriarPedidoAsync("PED-Y");

        // 10 pedidos criados E confirmados num ÚNICO SaveChanges: o EF Core não garante a ordem dos INSERTs
        // dentro do lote (a IDENTITY pode sair invertida). A ordem por pedido tem de vir da versão do agregado.
        await using (var escopo = Servicos.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();
            for (var i = 1; i <= 10; i++)
            {
                var pedido = Pedido.Criar($"PED-Z{i:00}", "z@cliente.test", 10m, Relogio.GetUtcNow());
                pedido.Confirmar(Relogio.GetUtcNow());
                db.Pedidos.Add(pedido);
            }
            await db.SaveChangesAsync();
        }

        // O "pedido.criado" de X falha na primeira tentativa (broker instável naquele instante).
        var publicador = new PublicadorQueGrava((m, tentativa) =>
            m.ChaveDeOrdenacao == x.ToString() && m.Tipo == PedidoCriado.NomeDoTipo && tentativa == 1
                ? new FalhaSimuladaException("timeout esperando o confirm")
                : null);
        var processor = CriarProcessor(publicador);

        await ProcessarAteEsvaziarAsync(processor);
        publicador.Publicadas.ShouldNotContain(m => m.ChaveDeOrdenacao == x.ToString(),
            "'pedido.confirmado' de X NÃO pode sair antes do 'pedido.criado' de X");
        publicador.Publicadas.ShouldContain(m => m.Payload.Contains("PED-Y"), "a falha em X não trava os outros pedidos");

        Relogio.Advance(TimeSpan.FromSeconds(10));
        await ProcessarAteEsvaziarAsync(processor);

        var deX = publicador.Publicadas.Where(m => m.ChaveDeOrdenacao == x.ToString()).Select(m => m.Tipo);
        deX.ShouldBe([PedidoCriado.NomeDoTipo, PedidoConfirmado.NomeDoTipo]);
        foreach (var historia in publicador.Publicadas.GroupBy(m => m.ChaveDeOrdenacao).Where(g => g.Count() > 1))
            historia.Select(m => m.Tipo).ShouldBe([PedidoCriado.NomeDoTipo, PedidoConfirmado.NomeDoTipo], $"ordem errada no pedido {historia.Key}");
        (await LerOutboxAsync()).ShouldAllBe(m => m.ProcessadoEm != null);
    }

    [Fact]
    public async Task ProcessarLote_DuasInstanciasAoMesmoTempo_NaoPublicamAMesmaMensagemDuasVezes()
    {
        for (var i = 1; i <= 6; i++) await CriarPedidoAsync($"PED-{i:0000}");

        using var portao = new PontoDeParada("instância A");
        var instanciaA = CriarProcessor(new PublicadorComPortao(Publicador, portao), o => o.TamanhoDoLote = 3);
        var instanciaB = CriarProcessor(Publicador, o => o.TamanhoDoLote = 50);

        // A reserva um lote de 3 e PARA na primeira publicação, com a transação (e os locks) abertos.
        var execucaoA = instanciaA.ProcessarLoteAsync();
        await portao.EsperarChegadaAsync(execucaoA);

        // B roda inteira enquanto A está parada: tem de PULAR as linhas de A (sem esperar, sem repetir).
        int publicadasPorB;
        try
        {
            publicadasPorB = await instanciaB.ProcessarLoteAsync().WaitAsync(TimeSpan.FromSeconds(15));
        }
        catch (TimeoutException)
        {
            throw new TimeoutException("A instância B ficou BLOQUEADA esperando os locks da A. Faltou READPAST na reserva?");
        }

        publicadasPorB.ShouldBe(3, "B pega só as 3 mensagens que A não reservou");

        portao.Liberar();
        (await execucaoA).ShouldBe(3);

        Publicador.Publicadas.Count.ShouldBe(6);
        Publicador.Publicadas.Select(m => m.MessageId).ShouldBeUnique();
        (await LerOutboxAsync()).ShouldAllBe(m => m.ProcessadoEm != null && m.Tentativas == 1);
    }

    [Fact]
    public async Task ProcessoCaiEntrePublicarEMarcar_ARodadaSeguinteRepublicaComOMesmoMessageId()
    {
        await CriarPedidoAsync("PED-0001");

        // "Queda" depois de publicar: o UPDATE que marcaria a mensagem como processada nunca chega ao banco.
        var queda = new FalharComandosInterceptor(
            sql => sql.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) && sql.Contains("OutboxMessages", StringComparison.Ordinal),
            motivo: "o processo caiu antes de marcar a mensagem");
        var servicosQueCaem = CriarServicos(dbPedidos: o => o.AddInterceptors(queda));

        var erro = await Should.ThrowAsync<Exception>(() => CriarProcessor(Publicador, servicos: servicosQueCaem).ProcessarLoteAsync());
        FalharComandosInterceptor.VeioDaSimulacao(erro).ShouldBeTrue($"a falha ao marcar deve SUBIR (sem commit), mas veio: {erro}");
        Publicador.Publicadas.Count.ShouldBe(1, "a mensagem chegou ao broker antes da queda");

        var depoisDaQueda = (await LerOutboxAsync()).ShouldHaveSingleItem();
        depoisDaQueda.ProcessadoEm.ShouldBeNull("a transação foi desfeita: para o banco, a mensagem nunca foi publicada");
        depoisDaQueda.Tentativas.ShouldBe(0);

        // O processo "reinicia": a mensagem sai DE NOVO. At-least-once — quem consome precisa deduplicar.
        (await CriarProcessor(Publicador).ProcessarLoteAsync()).ShouldBe(1);
        Publicador.Publicadas.Count.ShouldBe(2);
        Publicador.Publicadas.Select(m => m.MessageId).Distinct().ShouldHaveSingleItem().ShouldBe(depoisDaQueda.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ACadaTickDoRelogio_PublicaAsPendentesSemIntervencao()
    {
        var processor = CriarProcessor(Publicador); // Intervalo = 5 s (relógio falso)
        await processor.StartAsync(CancellationToken.None);
        try
        {
            await CriarPedidoAsync("PED-0001");
            await Esperas.Eventualmente(async () =>
            {
                Relogio.Advance(TimeSpan.FromSeconds(5)); // "passam 5 segundos"
                return Publicador.Publicadas.Count == 1 && await TodasProcessadasAsync(1);
            }, "o processor em segundo plano deveria publicar a cada tick do PeriodicTimer");

            await CriarPedidoAsync("PED-0002");
            await Esperas.Eventualmente(async () =>
            {
                Relogio.Advance(TimeSpan.FromSeconds(5));
                return Publicador.Publicadas.Count == 2 && await TodasProcessadasAsync(2);
            }, "o laço deveria continuar depois da primeira rodada");
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task LimparProcessadas_RemoveSoAsPublicadasHaMaisTempoQueARetencao()
    {
        await CriarPedidoAsync("PED-ANTIGO-1");
        await CriarPedidoAsync("PED-ANTIGO-2");
        var processor = CriarProcessor(Publicador); // retenção: 7 dias
        await ProcessarAteEsvaziarAsync(processor);

        Relogio.Advance(TimeSpan.FromDays(8));
        await CriarPedidoAsync("PED-RECENTE");
        await ProcessarAteEsvaziarAsync(processor);
        await CriarPedidoAsync("PED-PENDENTE"); // nunca publicada: não pode sumir

        (await processor.LimparProcessadasAsync()).ShouldBe(2);

        var restantes = await LerOutboxAsync();
        restantes.Count.ShouldBe(2);
        restantes.ShouldContain(m => m.ProcessadoEm != null && m.Payload.Contains("PED-RECENTE"));
        restantes.ShouldContain(m => m.ProcessadoEm == null && m.Payload.Contains("PED-PENDENTE"));
    }

    private async Task<bool> TodasProcessadasAsync(int quantidade)
    {
        var outbox = await LerOutboxAsync();
        return outbox.Count == quantidade && outbox.All(m => m.ProcessadoEm != null);
    }
}
