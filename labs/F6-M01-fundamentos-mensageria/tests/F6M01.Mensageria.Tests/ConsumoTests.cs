using System.Collections.Concurrent;
using F6M01.Mensageria.Consumo;
using F6M01.Mensageria.Contratos;
using F6M01.Mensageria.Publicacao;
using F6M01.Mensageria.Tests.Infra;
using F6M01.Mensageria.Topologia;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace F6M01.Mensageria.Tests;

/// <summary>Passos 6 a 8 — consumo com ack manual, prefetch, competing consumers e at-least-once (com broker).</summary>
public class ConsumoTests(RabbitFixture rabbit) : RabbitTestBase(rabbit)
{
    private const string Fila = TopologiaOrderFlow.FilaReservarEstoque;

    private static ReservarEstoque NovoComando(int n) => new(Guid.NewGuid(), [new ItemDaReserva($"SKU-{n}", n)]);

    private async Task<List<Envelope>> EnviarComandosAsync(int quantidade)
    {
        await using var publicador = await PublicadorDeMensagens.CriarAsync(Conexao);
        var enviados = new List<Envelope>();
        for (var i = 1; i <= quantidade; i++)
            enviados.Add(await publicador.EnviarAsync(NovoComando(i)));
        return enviados;
    }

    [Fact]
    public async Task Consumidor_ConfirmaAMensagemProcessada_EElaSaiDaFila()
    {
        await DeclararTopologiaAsync();
        var recebida = new TaskCompletionSource<MensagemRecebida>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var consumidor = await ConsumidorDeFila.IniciarAsync(Conexao, Fila, prefetch: 1, (m, _) =>
        {
            recebida.TrySetResult(m);
            return Task.FromResult(Decisao.Confirmar);
        });

        var enviado = (await EnviarComandosAsync(1)).Single();

        var mensagem = await Eventualmente.Aguardar(recebida.Task, "o consumidor não recebeu o comando");
        mensagem.Envelope.MessageId.ShouldBe(enviado.MessageId);
        mensagem.Reentregue.ShouldBeFalse();
        mensagem.Envelope.LerCorpo<ReservarEstoque>().Itens.ShouldHaveSingleItem().Sku.ShouldBe("SKU-1");

        await consumidor.DisposeAsync(); // fechar o canal devolveria o que NÃO foi confirmado
        (await Rabbit.ProntasNaFilaAsync(Fila)).ShouldBe(0u, "a mensagem foi confirmada (ack): o broker a apagou");
    }

    [Fact]
    public async Task DoisConsumidoresNaMesmaFila_DividemOTrabalho_SemDuplicar()
    {
        await DeclararTopologiaAsync();
        var processadas = new ConcurrentDictionary<Guid, string>();
        var emAndamento = 0;
        var doisEmParalelo = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<MensagemRecebida, CancellationToken, Task<Decisao>> Handler(string instancia) => async (m, _) =>
        {
            // As 2 primeiras mensagens só terminam quando AMBAS as instâncias estiverem trabalhando:
            // prova que o broker entregou para as duas (e não tudo para a primeira).
            if (Interlocked.Increment(ref emAndamento) >= 2)
                doisEmParalelo.TrySetResult();
            await doisEmParalelo.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            processadas.TryAdd(m.Envelope.MessageId, instancia).ShouldBeTrue("mensagem processada 2×");
            return Decisao.Confirmar;
        };

        await using var estoqueA = await ConsumidorDeFila.IniciarAsync(Conexao, Fila, prefetch: 1, Handler("A"));
        await using var estoqueB = await ConsumidorDeFila.IniciarAsync(Conexao, Fila, prefetch: 1, Handler("B"));

        var enviados = await EnviarComandosAsync(10);

        await Eventualmente.Ate(() => processadas.Count == 10, "as 10 reservas deveriam ser processadas");
        processadas.Keys.ShouldBe(enviados.Select(e => e.MessageId), ignoreOrder: true);
        processadas.Values.ShouldContain("A");
        processadas.Values.ShouldContain("B");
    }

    [Fact]
    public async Task Prefetch_LimitaQuantasMensagensSemAckUmConsumidorSegura()
    {
        await DeclararTopologiaAsync();
        await EnviarComandosAsync(10);
        var travaA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aRecebeu = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // A recebe e "trava" na primeira mensagem (processamento lento). Com prefetch 3, o broker
        // entrega a A no máximo 3 mensagens sem ack; as outras 7 ficam livres para B.
        await using var lento = await ConsumidorDeFila.IniciarAsync(Conexao, Fila, prefetch: 3, async (_, _) =>
        {
            aRecebeu.TrySetResult();
            await travaA.Task;
            return Decisao.Confirmar;
        });
        await Eventualmente.Aguardar(aRecebeu.Task.ContinueWith(_ => true), "o consumidor lento não recebeu nada");
        await Eventualmente.Ate(async () => await Rabbit.ProntasNaFilaAsync(Fila) == 7,
            "com prefetch 3, A deveria segurar 3 mensagens e deixar 7 prontas na fila");

        var recebidasPorB = new ConcurrentBag<Guid>();
        await using var rapido = await ConsumidorDeFila.IniciarAsync(Conexao, Fila, prefetch: 10, (m, _) =>
        {
            recebidasPorB.Add(m.Envelope.MessageId);
            return Task.FromResult(Decisao.Confirmar);
        });

        await Eventualmente.Ate(() => recebidasPorB.Count == 7, "B deveria receber exatamente as 7 que A não segurou");
        (await Rabbit.ProntasNaFilaAsync(Fila)).ShouldBe(0u);
        recebidasPorB.Count.ShouldBe(7);
        travaA.SetResult();
    }

    [Fact]
    public async Task ConsumidorCaiAntesDoAck_MensagemVoltaParaAFila_EOutroConsumidorRecebeComoReentregue()
    {
        await DeclararTopologiaAsync();
        var enviado = (await EnviarComandosAsync(1)).Single();

        // Instância A do Estoque: recebe a mensagem (autoAck: false) e o processo MORRE antes do ack.
        // Ela é escrita com a API crua do RabbitMQ.Client de propósito: é só um "processo" que vai cair.
        var conexaoDoA = await Rabbit.NovaConexaoAsync("estoque-instancia-A");
        var canalDoA = await conexaoDoA.CreateChannelAsync();
        await canalDoA.BasicQosAsync(0, 1, false);
        var aPegou = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var consumidorDoA = new AsyncEventingBasicConsumer(canalDoA);
        consumidorDoA.ReceivedAsync += (_, ea) =>
        {
            aPegou.TrySetResult(ea.BasicProperties.MessageId); // ...processando... (nunca confirma)
            return Task.CompletedTask;
        };
        await canalDoA.BasicConsumeAsync(Fila, autoAck: false, consumer: consumidorDoA);
        (await Eventualmente.Aguardar(aPegou.Task, "A não recebeu o comando")).ShouldBe(enviado.MessageId.ToString("D"));
        (await Rabbit.ProntasNaFilaAsync(Fila)).ShouldBe(0u, "entregue a A e ainda sem ack: não está 'pronta'");

        await conexaoDoA.AbortAsync(); // "kill -9": a conexão TCP cai sem ack
        await conexaoDoA.DisposeAsync();

        var recebidaPorB = new TaskCompletionSource<MensagemRecebida>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var instanciaB = await ConsumidorDeFila.IniciarAsync(Conexao, Fila, prefetch: 1, (m, _) =>
        {
            recebidaPorB.TrySetResult(m);
            return Task.FromResult(Decisao.Confirmar);
        });

        var mensagem = await Eventualmente.Aguardar(recebidaPorB.Task, "a mensagem sem ack não voltou para a fila");
        mensagem.Envelope.MessageId.ShouldBe(enviado.MessageId);
        mensagem.Reentregue.ShouldBeTrue("at-least-once: o broker avisa que pode ser uma 2ª entrega");
    }

    [Fact]
    public async Task Reprocessar_DevolveAMensagemParaAFila_ESegundaEntregaVemMarcadaComoReentregue()
    {
        await DeclararTopologiaAsync();
        var entregas = new ConcurrentQueue<MensagemRecebida>();
        await using var consumidor = await ConsumidorDeFila.IniciarAsync(Conexao, Fila, prefetch: 1, (m, _) =>
        {
            entregas.Enqueue(m);
            return Task.FromResult(entregas.Count == 1 ? Decisao.Reprocessar : Decisao.Confirmar);
        });

        await EnviarComandosAsync(1);

        await Eventualmente.Ate(() => entregas.Count == 2, "a mensagem devolvida (nack + requeue) deveria voltar");
        entregas.Select(e => e.Envelope.MessageId).Distinct().ShouldHaveSingleItem();
        entregas.Select(e => e.Reentregue).ShouldBe([false, true]);
    }

    [Fact]
    public async Task HandlerQueSempreFalha_TemUmaSegundaChance_EDepoisEDescartadoSemLoopInfinito()
    {
        await DeclararTopologiaAsync();
        var enviados = await EnviarComandosAsync(2); // [venenosa, sentinela]
        var venenosa = enviados[0].MessageId;
        var tentativasNaVenenosa = 0;
        var sentinelaProcessada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var consumidor = await ConsumidorDeFila.IniciarAsync(Conexao, Fila, prefetch: 1, (m, _) =>
        {
            if (m.Envelope.MessageId == venenosa)
            {
                Interlocked.Increment(ref tentativasNaVenenosa);
                throw new InvalidOperationException("bug no handler");
            }

            sentinelaProcessada.TrySetResult();
            return Task.FromResult(Decisao.Confirmar);
        });

        await Eventualmente.Aguardar(sentinelaProcessada.Task.ContinueWith(_ => true),
            "a fila travou na mensagem venenosa (loop de requeue?)");
        tentativasNaVenenosa.ShouldBe(2);
        (await Rabbit.ProntasNaFilaAsync(Fila)).ShouldBe(0u);
    }

    [Fact]
    public async Task MensagemSemEnvelopeValido_EhDescartada_EAFilaContinua()
    {
        await DeclararTopologiaAsync();
        await using (var canal = await Conexao.CreateChannelAsync())
        {
            // Alguém publicou "na mão", sem MessageId nem tipo.
            await canal.BasicPublishAsync(TopologiaOrderFlow.ExchangeComandos, Fila, "{\"oi\":1}"u8.ToArray());
        }

        var enviado = (await EnviarComandosAsync(1)).Single();
        var processadas = new ConcurrentQueue<Guid>();
        await using var consumidor = await ConsumidorDeFila.IniciarAsync(Conexao, Fila, prefetch: 1, (m, _) =>
        {
            processadas.Enqueue(m.Envelope.MessageId);
            return Task.FromResult(Decisao.Confirmar);
        });

        await Eventualmente.Ate(() => processadas.Count == 1, "o comando válido deveria ser processado");
        processadas.ToArray().ShouldBe([enviado.MessageId], "o handler nunca vê a mensagem sem envelope");
        await Eventualmente.Ate(async () => await Rabbit.ProntasNaFilaAsync(Fila) == 0, "a fila deveria esvaziar");
    }
}
