using MP3.Notificacoes.Tests.Infra;
using MP3.Notificacoes.Worker.Mensageria;

namespace MP3.Notificacoes.Tests;

/// <summary>Critério de aceite: a mesma mensagem entregue 2× gera 1 notificação.</summary>
[Collection(ColecaoAmbiente.Nome)]
public sealed class ConsumidorIdempotenteTests(AmbienteFixture ambiente)
{
    [Fact]
    public async Task PedidoCriado_EntregueUmaVez_GeraUmaNotificacaoERegistroNaInbox()
    {
        await ambiente.DrenarAsync();
        await using var worker = new WorkerFactory(ambiente).Iniciar();
        var evento = Novo.Pedido();

        await ambiente.PublicarAsync(Novo.Mensagem(evento));

        await Esperas.Eventualmente(() => worker.Observador.DesfechosDe(evento.EventoId).Count == 1, "mensagem liquidada");
        worker.Observador.DesfechosDe(evento.EventoId).ShouldBe([Desfecho.Notificada]);
        worker.Notificador.EnviadasPara(evento.EventoId).ShouldBe(1);
        (await worker.LinhasNaInboxAsync(evento.EventoId)).ShouldBe(1);
    }

    [Fact]
    public async Task MesmaMensagemEntregue2Vezes_GeraUmaNotificacao()
    {
        await ambiente.DrenarAsync();
        await using var worker = new WorkerFactory(ambiente).Iniciar();
        var evento = Novo.Pedido();

        // At-least-once: o publicador (Outbox) reenviou depois de um timeout. O broker entrega as duas.
        await ambiente.PublicarAsync(Novo.Mensagem(evento));
        await ambiente.PublicarAsync(Novo.Mensagem(evento));

        await Esperas.Eventualmente(() => worker.Observador.DesfechosDe(evento.EventoId).Count == 2, "as duas cópias liquidadas");
        worker.Observador.DesfechosDe(evento.EventoId).Order().ShouldBe([Desfecho.Notificada, Desfecho.Duplicada]);
        worker.Notificador.EnviadasPara(evento.EventoId).ShouldBe(1);
        (await worker.LinhasNaInboxAsync(evento.EventoId)).ShouldBe(1);
    }

    [Fact]
    public async Task CopiasConcorrentes_DoMesmoEvento_SoUmaNotifica()
    {
        await ambiente.DrenarAsync();
        await using var worker = new WorkerFactory(ambiente);
        var evento = Novo.Pedido();
        // A 1ª cópia "demora" no provedor: as outras chegam enquanto ela ainda segura a linha da inbox.
        var liberar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        worker.Notificador.Comportamento = async (_, tentativa, ct) =>
        {
            if (tentativa == 1) await liberar.Task.WaitAsync(ct);
        };
        worker.Iniciar();

        await ambiente.PublicarAsync(Novo.Mensagem(evento), Novo.Mensagem(evento), Novo.Mensagem(evento), Novo.Mensagem(evento));
        await Esperas.Eventualmente(() => worker.Notificador.TentativasPara(evento.EventoId) == 1, "1ª cópia no provedor");
        liberar.SetResult();

        await Esperas.Eventualmente(() => worker.Observador.DesfechosDe(evento.EventoId).Count == 4, "as 4 cópias liquidadas");
        worker.Observador.DesfechosDe(evento.EventoId).Count(d => d == Desfecho.Notificada).ShouldBe(1);
        worker.Observador.DesfechosDe(evento.EventoId).Count(d => d == Desfecho.Duplicada).ShouldBe(3);
        worker.Notificador.EnviadasPara(evento.EventoId).ShouldBe(1);
        worker.Notificador.TentativasPara(evento.EventoId).ShouldBe(1, "as cópias concorrentes esperaram o lock da inbox e nem chegaram ao provedor");
    }
}
