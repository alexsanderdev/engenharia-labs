using MP3.Notificacoes.Tests.Infra;
using MP3.Notificacoes.Worker.Mensageria;

namespace MP3.Notificacoes.Tests;

/// <summary>Critério de aceite: parar o worker no meio do processamento não perde mensagens.</summary>
[Collection(ColecaoAmbiente.Nome)]
public sealed class ShutdownTests(AmbienteFixture ambiente)
{
    [Fact]
    public async Task PararNoMeioDoEnvio_MensagemVoltaParaAFila_EOutraInstanciaNotificaUmaVez()
    {
        await ambiente.DrenarAsync();
        var evento = Novo.Pedido();
        var noProvedor = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Instância 1: o envio "trava" até o desligamento cancelar o token.
        var primeira = new WorkerFactory(ambiente);
        primeira.Notificador.Comportamento = async (_, _, ct) =>
        {
            noProvedor.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
        };
        primeira.Iniciar();
        await ambiente.PublicarAsync(Novo.Mensagem(evento));
        await Esperas.Sinal(noProvedor.Task, "mensagem chegou ao provedor");

        await primeira.DisposeAsync(); // SIGTERM: StopAsync do host → StopProcessingAsync do processor

        primeira.Observador.DesfechosDe(evento.EventoId).ShouldBe([Desfecho.Abandonada]);
        primeira.Notificador.EnviadasPara(evento.EventoId).ShouldBe(0);
        (await primeira.LinhasNaInboxAsync(evento.EventoId)).ShouldBe(0, "a transação da inbox foi desfeita");

        // Instância 2 (o pod novo): recebe a mesma mensagem e notifica uma única vez.
        await using var segunda = new WorkerFactory(ambiente).Iniciar();
        await Esperas.Eventualmente(() => segunda.Notificador.EnviadasPara(evento.EventoId) == 1, "a 2ª instância notificou");
        segunda.Observador.DesfechosDe(evento.EventoId).ShouldBe([Desfecho.Notificada]);
        (await segunda.LinhasNaInboxAsync(evento.EventoId)).ShouldBe(1);
    }
}
