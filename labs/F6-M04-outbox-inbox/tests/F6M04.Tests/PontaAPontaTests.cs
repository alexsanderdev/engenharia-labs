using F6M04.Notificacoes.Mensageria;
using F6M04.Pedidos.Outbox;
using F6M04.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;

namespace F6M04.Tests;

/// <summary>Passo 8 — ponta a ponta: Pedidos → Outbox → RabbitMQ → worker → Inbox → notificação.</summary>
[Collection(ColecaoInfra.Nome)]
public sealed class PontaAPontaTests(InfraFixture infra) : TesteComInfra(infra)
{
    private static async Task<WorkerDeNotificacoes> IniciarWorkerAsync(IServiceProvider servicos)
    {
        var worker = servicos.GetRequiredService<WorkerDeNotificacoes>();
        await worker.StartAsync(CancellationToken.None);
        await worker.Pronto.WaitAsync(TimeSpan.FromSeconds(15));
        return worker;
    }

    [Fact]
    public async Task PedidoCriadoEConfirmado_ViajaPeloRabbitMqEGeraDuasNotificacoes()
    {
        var servicos = CriarServicos(brokerReal: true);
        var worker = await IniciarWorkerAsync(servicos);
        try
        {
            var pedidoId = await CriarPedidoAsync("PED-0001", servicos: servicos);
            await ConfirmarPedidoAsync(pedidoId, servicos);
            await ProcessarAteEsvaziarAsync(servicos.GetRequiredService<OutboxProcessor>());

            await Esperas.Eventualmente(() => worker.Processadas == 2, "o worker deveria processar 'criado' e 'confirmado'");

            var notificacoes = await LerNotificacoesAsync();
            notificacoes.Select(n => n.Tipo).ShouldBe(["pedido.confirmado", "pedido.criado"], ignoreOrder: true);
            notificacoes.ShouldAllBe(n => n.PedidoId == pedidoId);
            (await LerInboxAsync()).Count.ShouldBe(2);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ProcessoCaiEntrePublicarEMarcar_OConsumidorRecebeDuasVezesEAplicaOEfeitoUmaVez()
    {
        var servicos = CriarServicos(brokerReal: true);
        var queda = new FalharComandosInterceptor(
            sql => sql.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) && sql.Contains("OutboxMessages", StringComparison.Ordinal),
            motivo: "o processo caiu antes de marcar a mensagem");
        var servicosQueCaem = CriarServicos(brokerReal: true, dbPedidos: o => o.AddInterceptors(queda));

        var worker = await IniciarWorkerAsync(servicos);
        try
        {
            await CriarPedidoAsync("PED-0001", servicos: servicos);

            // 1ª instância: publica no RabbitMQ (com confirm) e "morre" antes do commit.
            var erro = await Should.ThrowAsync<Exception>(() => servicosQueCaem.GetRequiredService<OutboxProcessor>().ProcessarLoteAsync());
            FalharComandosInterceptor.VeioDaSimulacao(erro).ShouldBeTrue($"esperava a queda simulada, veio: {erro}");

            // Instância reiniciada: a mensagem continua pendente e sai de novo (mesmo MessageId).
            (await servicos.GetRequiredService<OutboxProcessor>().ProcessarLoteAsync()).ShouldBe(1);

            await Esperas.Eventualmente(() => worker.Processadas + worker.Duplicadas == 2, "o worker deveria receber as DUAS cópias");
            worker.Processadas.ShouldBe(1);
            worker.Duplicadas.ShouldBe(1);

            (await LerNotificacoesAsync()).Count.ShouldBe(1, "at-least-once na entrega + Inbox = efeito exatamente uma vez");
            (await LerInboxAsync()).Count.ShouldBe(1);
            (await Broker.ContarAsync(Topologia.Fila)).ShouldBe(0u, "a duplicata também recebe ack (senão volta para sempre)");
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Worker_MensagemIlegivelOuSemMessageId_VaiParaADlqEOWorkerSegueProcessando()
    {
        var servicos = CriarServicos(brokerReal: true);
        var worker = await IniciarWorkerAsync(servicos);
        try
        {
            await Broker.PublicarCruAsync("pedido.criado", "lixo-1", "{ isto não é json");
            await Broker.PublicarCruAsync("pedido.criado", messageId: null,
                """{"pedidoId":"0199c3f0-0000-7000-8000-000000000001","numero":"PED-9","clienteEmail":"x@cliente.test","total":10}""");
            await Broker.PublicarCruAsync("pedido.criado", "boa-1",
                """{"pedidoId":"0199c3f0-0000-7000-8000-000000000002","numero":"PED-0001","clienteEmail":"ana@cliente.test","total":10}""");

            await Esperas.Eventualmente(() => worker.Rejeitadas == 2 && worker.Processadas == 1,
                "duas mensagens deveriam ir para a DLQ e a boa deveria ser processada");
            await Esperas.Eventualmente(async () => await Broker.ContarAsync(Topologia.FilaDeMensagensMortas) == 2,
                "as duas mensagens inválidas deveriam estar na DLQ");

            (await LerNotificacoesAsync()).ShouldHaveSingleItem().Texto.ShouldContain("PED-0001");
            worker.Devolvidas.ShouldBe(0, "mensagem que nunca vai funcionar não volta para a fila: vai direto para a DLQ");
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }
}
