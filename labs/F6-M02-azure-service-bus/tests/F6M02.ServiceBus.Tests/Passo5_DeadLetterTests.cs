using F6M02.ServiceBus.Consumo;
using F6M02.ServiceBus.Publicacao;
using F6M02.ServiceBus.Tests.Infra;
using F6M02.ServiceBus.Topologia;

namespace F6M02.ServiceBus.Tests;

/// <summary>
/// Passo 5 — operar a DLQ (<c>SubQueue.DeadLetter</c>): enxergar por que a mensagem morreu e,
/// depois de corrigir a causa, devolvê-la para a fila (redrive).
/// </summary>
[Collection(ColecaoServiceBus.Nome)]
public sealed class Passo5_DeadLetterTests(ServiceBusFixture fixture) : IAsyncLifetime
{
    private static readonly OrigemDasMensagens Fila = OrigemDasMensagens.Fila(Entidades.FilaProcessamento);

    public async ValueTask InitializeAsync() => await fixture.DrenarAsync(Fila);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>Coloca uma mensagem na DLQ do jeito "de verdade": um consumidor que a rejeita.</summary>
    private async Task<string> MatarUmaMensagemAsync(string motivo, string descricao)
    {
        var mensagem = MensagensDePedido.CriarPedidoCriado(Novo.Pedido(), Novo.CorrelationId());
        await fixture.EnviarAsync(Entidades.FilaProcessamento, mensagem);

        await using var consumidor = new ConsumidorDePedidos(fixture.Cliente, Fila, (_, _, _) =>
            Task.FromResult<ResultadoDoProcessamento>(new ResultadoDoProcessamento.FalhaPermanente(motivo, descricao)));
        await consumidor.IniciarAsync();

        var leitor = new LeitorDeDeadLetter(fixture.Cliente);
        await Esperas.Eventualmente(
            async () => (await leitor.InspecionarAsync(Fila)).Any(m => m.MessageId == mensagem.MessageId),
            "a mensagem deveria chegar à DLQ");
        await consumidor.PararAsync();
        return mensagem.MessageId;
    }

    [Fact]
    public async Task Inspecionar_MostraMotivoDescricaoECorpoSemRemoverDaDlq()
    {
        var messageId = await MatarUmaMensagemAsync("ProdutoDescontinuado", "SKU 42 saiu do catálogo");
        var leitor = new LeitorDeDeadLetter(fixture.Cliente);

        var primeira = (await leitor.InspecionarAsync(Fila)).Single(m => m.MessageId == messageId);
        var segunda = (await leitor.InspecionarAsync(Fila)).Single(m => m.MessageId == messageId);

        primeira.Motivo.ShouldBe("ProdutoDescontinuado");
        primeira.Descricao.ShouldBe("SKU 42 saiu do catálogo");
        primeira.Corpo.ShouldContain("\"pedidoId\"");
        primeira.SequenceNumber.ShouldBeGreaterThan(0);
        segunda.SequenceNumber.ShouldBe(primeira.SequenceNumber, "inspecionar (peek) não pode remover a mensagem da DLQ");
    }

    [Fact]
    public async Task Reenviar_DevolveAMensagemParaAFilaComCarimboEElaEhProcessada()
    {
        var messageId = await MatarUmaMensagemAsync("EstoqueInexistente", "depósito ainda não cadastrado");
        var leitor = new LeitorDeDeadLetter(fixture.Cliente);

        // A causa foi corrigida (depósito cadastrado): hora do redrive.
        var reenviadas = await leitor.ReenviarAsync(Entidades.FilaProcessamento, maximo: 10, esperaMaxima: TimeSpan.FromSeconds(2));
        reenviadas.ShouldBe(1);

        var processada = new TaskCompletionSource<ContextoDaMensagem>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var consumidor = new ConsumidorDePedidos(fixture.Cliente, Fila, (_, ctx, _) =>
        {
            if (ctx.MessageId == messageId) processada.TrySetResult(ctx);
            return Task.FromResult(ResultadoDoProcessamento.Ok);
        });
        await consumidor.IniciarAsync();

        var contexto = await Esperas.Sinal(processada, "a mensagem reenviada deveria ser processada");
        await consumidor.PararAsync();

        contexto.Propriedades.ShouldNotBeNull();
        contexto.Propriedades![LeitorDeDeadLetter.PropriedadeReenvios].ShouldBe(1);
        contexto.Propriedades.ContainsKey("DeadLetterReason").ShouldBeFalse("a cópia não deve carregar o motivo da morte anterior");
        (await leitor.InspecionarAsync(Fila)).ShouldNotContain(m => m.MessageId == messageId, "a mensagem deve sair da DLQ");
    }
}
