using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Consumo;
using F6M02.ServiceBus.Contratos;
using F6M02.ServiceBus.Pagamentos;
using F6M02.ServiceBus.Tests.Infra;
using F6M02.ServiceBus.Topologia;

namespace F6M02.ServiceBus.Tests;

/// <summary>
/// Passo 7 — recursos do broker que poupam código: detecção de duplicatas (fila <c>pagamentos</c>)
/// e mensagens agendadas (fila <c>lembretes</c>).
/// </summary>
[Collection(ColecaoServiceBus.Nome)]
public sealed class Passo7_DuplicatasEAgendamentoTests(ServiceBusFixture fixture)
{
    private static readonly OrigemDasMensagens Pagamentos = OrigemDasMensagens.Fila(Entidades.FilaPagamentos);
    private static readonly OrigemDasMensagens Lembretes = OrigemDasMensagens.Fila(Entidades.FilaLembretes);

    [Fact]
    public async Task SolicitarCobranca_MesmoPedidoDuasVezes_BrokerDescartaADuplicata()
    {
        await fixture.DrenarAsync(Pagamentos);
        await using var publicador = new PublicadorDePagamentos(fixture.Cliente);
        var pedidoA = new SolicitacaoDeCobranca(Guid.NewGuid(), 250m);
        var sentinela = new SolicitacaoDeCobranca(Guid.NewGuid(), 99m);

        await publicador.SolicitarCobrancaAsync(pedidoA);
        await publicador.SolicitarCobrancaAsync(pedidoA); // retry do publicador / clique duplo
        await publicador.SolicitarCobrancaAsync(sentinela);

        var recebidas = await Esperas.ReceberAte(fixture.Cliente, Pagamentos,
            r => r.Any(m => m.MessageId == $"cobranca-{sentinela.PedidoId:N}"));

        recebidas.Select(m => m.MessageId).ShouldBe(
            [$"cobranca-{pedidoA.PedidoId:N}", $"cobranca-{sentinela.PedidoId:N}"],
            "com RequiresDuplicateDetection, o mesmo MessageId dentro da janela é descartado pelo broker");
    }

    [Fact]
    public async Task AgendarLembrete_MensagemSoFicaDisponivelNoHorarioAgendado()
    {
        await fixture.DrenarAsync(Lembretes);
        await using var agendador = new AgendadorDeLembretes(fixture.Cliente);
        var lembrete = new LembreteDePagamento(Guid.NewGuid(), Guid.NewGuid());
        var quando = DateTimeOffset.UtcNow.AddSeconds(4);

        var sequencia = await agendador.AgendarAsync(lembrete, quando);

        await using var receiver = Lembretes.CriarReceiver(fixture.Cliente);

        // Já existe no broker (peek enxerga), mas no estado "Scheduled".
        var espiada = (await receiver.PeekMessagesAsync(50)).Single(m => m.MessageId == $"lembrete-{lembrete.PedidoId:N}");
        espiada.State.ShouldBe(ServiceBusMessageState.Scheduled);
        espiada.SequenceNumber.ShouldBe(sequencia);
        (espiada.ScheduledEnqueueTime - quando).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(1));

        // Antes do horário, ninguém recebe.
        (await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1))).ShouldBeNull("mensagem agendada não pode ser entregue antes do horário");

        // No horário, chega (o receive espera até 15 s; não há sleep fixo).
        var recebida = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(15));
        recebida.ShouldNotBeNull();
        recebida.MessageId.ShouldBe($"lembrete-{lembrete.PedidoId:N}");
        recebida.EnqueuedTime.ShouldBeGreaterThanOrEqualTo(quando.AddSeconds(-1));
        await receiver.CompleteMessageAsync(recebida);
    }
}
