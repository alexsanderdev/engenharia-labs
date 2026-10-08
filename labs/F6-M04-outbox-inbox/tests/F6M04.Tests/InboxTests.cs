using System.Text.Json;
using F6M04.Notificacoes.Contratos;
using F6M04.Notificacoes.Inbox;
using F6M04.Tests.Infra;

namespace F6M04.Tests;

/// <summary>Passo 7 — o consumidor idempotente: Inbox na mesma transação do efeito.</summary>
[Collection(ColecaoInfra.Nome)]
public sealed class InboxTests(InfraFixture infra) : TesteComInfra(infra)
{
    private static MensagemRecebida PedidoCriado(string messageId, Guid pedidoId, string numero = "PED-0001") =>
        new(messageId, PedidoCriadoRecebido.Tipo,
            JsonSerializer.Serialize(new { pedidoId, numero, clienteEmail = "ana@cliente.test", total = 150m, ocorridoEm = DateTimeOffset.UnixEpoch },
                JsonSerializerOptions.Web));

    [Fact]
    public async Task Processar_PrimeiraEntrega_GravaANotificacaoEOMessageIdNaInbox()
    {
        var pedidoId = Guid.NewGuid();

        var resultado = await ConsumirAsync(Servicos, PedidoCriado("msg-1", pedidoId));

        resultado.ShouldBe(ResultadoDoProcessamento.Processada);
        var notificacao = (await LerNotificacoesAsync()).ShouldHaveSingleItem();
        notificacao.PedidoId.ShouldBe(pedidoId);
        notificacao.Destinatario.ShouldBe("ana@cliente.test");
        notificacao.Texto.ShouldContain("PED-0001");

        var inbox = (await LerInboxAsync()).ShouldHaveSingleItem();
        inbox.MessageId.ShouldBe("msg-1");
        inbox.Consumidor.ShouldBe(ConsumidorDeNotificacoes.Nome);
        inbox.ProcessadaEm.ShouldBe(Relogio.GetUtcNow());
    }

    [Fact]
    public async Task Processar_MesmaMensagemDuasVezes_ReconheceADuplicadaSemRepetirOEfeito()
    {
        var mensagem = PedidoCriado("msg-1", Guid.NewGuid());

        (await ConsumirAsync(Servicos, mensagem)).ShouldBe(ResultadoDoProcessamento.Processada);
        (await ConsumirAsync(Servicos, mensagem)).ShouldBe(ResultadoDoProcessamento.Duplicada);

        (await LerNotificacoesAsync()).Count.ShouldBe(1, "o cliente não pode receber o mesmo e-mail duas vezes");
        (await LerInboxAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Processar_EntregasSimultaneasDaMesmaMensagem_OEfeitoAconteceUmaVezSo()
    {
        var mensagem = PedidoCriado("msg-concorrente", Guid.NewGuid());
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // 8 "réplicas do worker" recebem a mesma mensagem ao mesmo tempo (ex.: reentrega depois de queda de conexão).
        var execucoes = Enumerable.Range(0, 8).Select(async _ =>
        {
            await largada.Task;
            return await ConsumirAsync(Servicos, mensagem);
        }).ToList();
        largada.SetResult();
        var resultados = await Task.WhenAll(execucoes).WaitAsync(TimeSpan.FromSeconds(30));

        resultados.Count(r => r == ResultadoDoProcessamento.Processada).ShouldBe(1);
        resultados.Count(r => r == ResultadoDoProcessamento.Duplicada).ShouldBe(7, "a corrida perdida (violação da PK da Inbox) também é duplicata, não erro");
        (await LerNotificacoesAsync()).Count.ShouldBe(1);
        (await LerInboxAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Processar_FalhaAoGravar_NadaFicaGravadoEAReentregaProcessaNormalmente()
    {
        // O banco "cai" no INSERT da Inbox (uma vez). Efeito e Inbox são uma coisa só: nenhum dos dois pode sobrar.
        var falha = new FalharComandosInterceptor(
            sql => sql.Contains("INSERT", StringComparison.OrdinalIgnoreCase) && sql.Contains("InboxMessages", StringComparison.Ordinal),
            vezes: 1,
            motivo: "conexão perdida durante o commit");
        var servicosComFalha = CriarServicos(dbNotificacoes: o => o.AddInterceptors(falha));
        var mensagem = PedidoCriado("msg-1", Guid.NewGuid());

        var erro = await Should.ThrowAsync<Exception>(() => ConsumirAsync(servicosComFalha, mensagem));
        FalharComandosInterceptor.VeioDaSimulacao(erro).ShouldBeTrue($"esperava a falha simulada, veio: {erro}");
        (await LerNotificacoesAsync()).ShouldBeEmpty("se a Inbox não foi gravada, o efeito também não pode ter sido");
        (await LerInboxAsync()).ShouldBeEmpty("se a Inbox ficasse gravada, a reentrega seria descartada e o e-mail nunca sairia");

        // O broker reentrega (não houve ack): agora funciona, uma vez só.
        (await ConsumirAsync(servicosComFalha, mensagem)).ShouldBe(ResultadoDoProcessamento.Processada);
        (await LerNotificacoesAsync()).Count.ShouldBe(1);
    }
}
