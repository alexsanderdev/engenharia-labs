using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Consumo;
using F6M02.ServiceBus.Publicacao;
using F6M02.ServiceBus.Tests.Infra;
using F6M02.ServiceBus.Topologia;

namespace F6M02.ServiceBus.Tests;

/// <summary>
/// Passo 4 — consumir com <c>ServiceBusProcessor</c> em peek-lock e liquidar cada mensagem do jeito certo:
/// complete, abandon (redelivery) ou dead-letter. Fila: <c>pedidos-processamento</c> (MaxDeliveryCount = 3).
/// </summary>
[Collection(ColecaoServiceBus.Nome)]
public sealed class Passo4_ConsumoPeekLockTests(ServiceBusFixture fixture) : IAsyncLifetime
{
    private static readonly OrigemDasMensagens Fila = OrigemDasMensagens.Fila(Entidades.FilaProcessamento);

    public async ValueTask InitializeAsync() => await fixture.DrenarAsync(Fila);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<string> EnviarPedidoAsync(decimal valor = 150m)
    {
        var mensagem = MensagensDePedido.CriarPedidoCriado(Novo.Pedido(valor), Novo.CorrelationId());
        await fixture.EnviarAsync(Entidades.FilaProcessamento, mensagem);
        return mensagem.MessageId;
    }

    private async Task<bool> FilaSemMensagemAsync(string messageId)
    {
        await using var receiver = Fila.CriarReceiver(fixture.Cliente);
        var visiveis = await receiver.PeekMessagesAsync(100);
        return visiveis.All(m => m.MessageId != messageId);
    }

    [Fact]
    public async Task Consumidor_HandlerComSucesso_CompletaEAMensagemSaiDaFila()
    {
        var messageId = await EnviarPedidoAsync();
        var processada = new TaskCompletionSource<ContextoDaMensagem>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var consumidor = new ConsumidorDePedidos(fixture.Cliente, Fila, (evento, ctx, ct) =>
        {
            if (ctx.MessageId == messageId) processada.TrySetResult(ctx);
            return Task.FromResult(ResultadoDoProcessamento.Ok);
        });
        await consumidor.IniciarAsync();

        var contexto = await Esperas.Sinal(processada, "o handler deveria receber a mensagem");
        contexto.DeliveryCount.ShouldBe(1);
        contexto.CorrelationId.ShouldNotBeNullOrEmpty();

        await Esperas.Eventualmente(() => FilaSemMensagemAsync(messageId), "a mensagem completada deveria sair da fila");
        await consumidor.PararAsync();
        consumidor.Erros.ShouldBeEmpty();
    }

    [Fact]
    public async Task Consumidor_FalhaTransitoria_AbandonaEAMensagemVoltaComDeliveryCountMaior()
    {
        var messageId = await EnviarPedidoAsync();
        var entregas = new ConcurrentQueue<ContextoDaMensagem>();
        var sucesso = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var consumidor = new ConsumidorDePedidos(fixture.Cliente, Fila, (evento, ctx, ct) =>
        {
            if (ctx.MessageId != messageId) return Task.FromResult(ResultadoDoProcessamento.Ok);
            entregas.Enqueue(ctx);
            if (ctx.DeliveryCount == 1)
                return Task.FromResult<ResultadoDoProcessamento>(new ResultadoDoProcessamento.FalhaTransitoria("estoque indisponível"));
            sucesso.TrySetResult();
            return Task.FromResult(ResultadoDoProcessamento.Ok);
        });
        await consumidor.IniciarAsync();

        await Esperas.Sinal(sucesso, "depois do abandon, a mensagem deveria ser entregue de novo");
        await consumidor.PararAsync();

        entregas.Select(e => e.DeliveryCount).ShouldBe([1, 2]);
        var segunda = entregas.Last();
        segunda.Propriedades.ShouldNotBeNull();
        segunda.Propriedades![ConsumidorDePedidos.PropriedadeUltimoMotivo].ShouldBe("estoque indisponível",
            "o abandon deveria gravar o motivo nas application properties (propertiesToModify)");
        await Esperas.Eventualmente(() => FilaSemMensagemAsync(messageId), "a mensagem deveria sair da fila no segundo processamento");
    }

    [Fact]
    public async Task Consumidor_FalhaPermanente_VaiDiretoParaDlqComMotivoEDescricao()
    {
        var messageId = await EnviarPedidoAsync();
        var chamadas = 0;

        await using var consumidor = new ConsumidorDePedidos(fixture.Cliente, Fila, (evento, ctx, ct) =>
        {
            Interlocked.Increment(ref chamadas);
            return Task.FromResult<ResultadoDoProcessamento>(
                new ResultadoDoProcessamento.FalhaPermanente("ClienteBloqueado", $"Cliente {evento.ClienteId} bloqueado por inadimplência"));
        });
        await consumidor.IniciarAsync();

        var leitor = new LeitorDeDeadLetter(fixture.Cliente);
        MensagemMorta? morta = null;
        await Esperas.Eventualmente(async () =>
        {
            morta = (await leitor.InspecionarAsync(Fila)).FirstOrDefault(m => m.MessageId == messageId);
            return morta is not null;
        }, "a mensagem deveria estar na DLQ");
        await consumidor.PararAsync();

        morta!.Motivo.ShouldBe("ClienteBloqueado");
        morta.Descricao.ShouldNotBeNull().ShouldContain("bloqueado por inadimplência");
        chamadas.ShouldBe(1, "falha permanente não gasta retentativas");
    }

    [Fact]
    public async Task Consumidor_CorpoInvalido_VaiParaDlqComoMensagemInvalidaSemChamarOHandler()
    {
        var messageId = $"lixo-{Guid.NewGuid():N}";
        await fixture.EnviarAsync(Entidades.FilaProcessamento,
            new ServiceBusMessage(BinaryData.FromString("<pedido>xml?</pedido>")) { MessageId = messageId, ContentType = "application/json" });
        var handlerChamado = false;

        await using var consumidor = new ConsumidorDePedidos(fixture.Cliente, Fila, (evento, ctx, ct) =>
        {
            handlerChamado = true;
            return Task.FromResult(ResultadoDoProcessamento.Ok);
        });
        await consumidor.IniciarAsync();

        var leitor = new LeitorDeDeadLetter(fixture.Cliente);
        MensagemMorta? morta = null;
        await Esperas.Eventualmente(async () =>
        {
            morta = (await leitor.InspecionarAsync(Fila)).FirstOrDefault(m => m.MessageId == messageId);
            return morta is not null;
        }, "a mensagem venenosa deveria ir para a DLQ");
        await consumidor.PararAsync();

        morta!.Motivo.ShouldBe(MotivosDeDeadLetter.MensagemInvalida);
        // No peek da DLQ o emulador mostra 0 (o receive mostraria 1): o que importa é que não houve retentativa.
        morta.DeliveryCount.ShouldBeLessThanOrEqualTo(1, "mensagem venenosa não deve ser retentada");
        handlerChamado.ShouldBeFalse();
    }

    [Fact]
    public async Task Consumidor_HandlerSempreLanca_BrokerMoveParaDlqAoExcederMaxDeliveryCount()
    {
        var messageId = await EnviarPedidoAsync();
        var tentativas = 0;

        await using var consumidor = new ConsumidorDePedidos(fixture.Cliente, Fila, (evento, ctx, ct) =>
        {
            if (ctx.MessageId == messageId) Interlocked.Increment(ref tentativas);
            throw new TimeoutException("banco de estoque não respondeu");
        });
        await consumidor.IniciarAsync();

        var leitor = new LeitorDeDeadLetter(fixture.Cliente);
        MensagemMorta? morta = null;
        await Esperas.Eventualmente(async () =>
        {
            morta = (await leitor.InspecionarAsync(Fila)).FirstOrDefault(m => m.MessageId == messageId);
            return morta is not null;
        }, "ao exceder o MaxDeliveryCount o broker deveria mover a mensagem para a DLQ");
        await consumidor.PararAsync();

        morta!.Motivo.ShouldBe(MotivosDeDeadLetter.MaxDeliveryCountExceeded, "quem dá esse motivo é o broker, não o seu código");
        tentativas.ShouldBe(Entidades.MaxDeliveryCountProcessamento);
    }

    [Fact]
    public async Task Consumidor_MaxConcurrentCalls_ProcessaVariasMensagensAoMesmoTempo()
    {
        var opcoes = ConsumidorDePedidos.CriarOpcoesDoProcessor(new ConsumidorDePedidosOptions { MaxConcurrentCalls = 4 });
        opcoes.ReceiveMode.ShouldBe(ServiceBusReceiveMode.PeekLock);
        opcoes.AutoCompleteMessages.ShouldBeFalse("quem liquida a mensagem é o consumidor, conforme o resultado");
        opcoes.MaxConcurrentCalls.ShouldBe(4);
        opcoes.PrefetchCount.ShouldBe(0);

        var ids = new HashSet<string>();
        for (var i = 0; i < 4; i++) ids.Add(await EnviarPedidoAsync());

        var emAndamento = 0;
        var maximoSimultaneo = 0;
        var concluidas = 0;
        var todasDentro = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var todasConcluidas = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var consumidor = new ConsumidorDePedidos(fixture.Cliente, Fila, async (evento, ctx, ct) =>
        {
            if (!ids.Contains(ctx.MessageId)) return ResultadoDoProcessamento.Ok;

            var agora = Interlocked.Increment(ref emAndamento);
            InterlockedMax(ref maximoSimultaneo, agora);
            if (agora == 4) todasDentro.TrySetResult();

            // Segura o handler até as 4 estarem dentro ao mesmo tempo (ou desiste em 10 s: com 1 por vez, nunca chegariam).
            try { await todasDentro.Task.WaitAsync(TimeSpan.FromSeconds(10), ct); }
            catch (TimeoutException) { }

            Interlocked.Decrement(ref emAndamento);
            if (Interlocked.Increment(ref concluidas) == 4) todasConcluidas.TrySetResult();
            return ResultadoDoProcessamento.Ok;
        }, new ConsumidorDePedidosOptions { MaxConcurrentCalls = 4 });
        await consumidor.IniciarAsync();

        await Esperas.Sinal(todasConcluidas, "as 4 mensagens deveriam ser processadas", TimeSpan.FromSeconds(60));
        await consumidor.PararAsync();

        maximoSimultaneo.ShouldBe(4, "com MaxConcurrentCalls = 4 o processor chama o handler em paralelo");
    }

    private static void InterlockedMax(ref int alvo, int valor)
    {
        int atual;
        while (valor > (atual = Volatile.Read(ref alvo)) && Interlocked.CompareExchange(ref alvo, valor, atual) != atual)
        {
        }
    }
}
