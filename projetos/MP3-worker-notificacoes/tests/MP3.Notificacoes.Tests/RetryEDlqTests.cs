using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using MP3.Notificacoes.Tests.Infra;
using MP3.Notificacoes.Worker.Mensageria;
using MP3.Notificacoes.Worker.Notificacoes;

namespace MP3.Notificacoes.Tests;

/// <summary>Critério de aceite: falha transitória → retry → sucesso; falha permanente → DLQ (e o comando de reprocessar).</summary>
[Collection(ColecaoAmbiente.Nome)]
public sealed class RetryEDlqTests(AmbienteFixture ambiente)
{
    [Fact]
    public async Task FalhaTransitoria_RetryComBackoff_DepoisSucesso()
    {
        await ambiente.DrenarAsync();
        await using var worker = new WorkerFactory(ambiente);
        worker.Notificador.Comportamento = (_, tentativa, _) =>
            tentativa <= 2 ? throw new TimeoutException($"provedor lento (tentativa {tentativa})") : Task.CompletedTask;
        worker.Iniciar();
        var evento = Novo.Pedido();

        await ambiente.PublicarAsync(Novo.Mensagem(evento));

        await Esperas.Eventualmente(() => worker.Notificador.EnviadasPara(evento.EventoId) == 1, "notificação após os retries");
        worker.Notificador.TentativasPara(evento.EventoId).ShouldBe(3);
        worker.Observador.DesfechosDe(evento.EventoId).ShouldBe([Desfecho.Reagendada, Desfecho.Reagendada, Desfecho.Notificada]);
        (await ambiente.NaDlqAsync(evento.EventoId)).ShouldBeNull();
        (await worker.LinhasNaInboxAsync(evento.EventoId)).ShouldBe(1);
    }

    [Fact]
    public async Task FalhaPermanente_VaiDiretoParaADlqSemRetry()
    {
        await ambiente.DrenarAsync();
        await using var worker = new WorkerFactory(ambiente);
        worker.Notificador.Comportamento = (_, _, _) => throw new FalhaPermanenteException("destino bloqueado pelo provedor");
        worker.Iniciar();
        var evento = Novo.Pedido();

        await ambiente.PublicarAsync(Novo.Mensagem(evento));

        await Esperas.Eventualmente(async () => await ambiente.NaDlqAsync(evento.EventoId) is not null, "mensagem na DLQ");
        var morta = (await ambiente.NaDlqAsync(evento.EventoId))!;
        morta.DeadLetterReason.ShouldBe("FalhaPermanente");
        morta.DeadLetterErrorDescription.ShouldContain("destino bloqueado");
        worker.Notificador.TentativasPara(evento.EventoId).ShouldBe(1);
        worker.Notificador.EnviadasPara(evento.EventoId).ShouldBe(0);
        (await worker.LinhasNaInboxAsync(evento.EventoId)).ShouldBe(0, "a transação da inbox foi desfeita");
    }

    [Fact]
    public async Task FalhaTransitoriaQueNaoPassa_EsgotaAsTentativasEVaiParaADlq()
    {
        await ambiente.DrenarAsync();
        await using var worker = new WorkerFactory(ambiente);
        worker.Notificador.Comportamento = (_, _, _) => throw new HttpRequestException("503 do provedor");
        worker.Iniciar();
        var evento = Novo.Pedido();

        await ambiente.PublicarAsync(Novo.Mensagem(evento));

        await Esperas.Eventualmente(async () => await ambiente.NaDlqAsync(evento.EventoId) is not null, "mensagem na DLQ");
        (await ambiente.NaDlqAsync(evento.EventoId))!.DeadLetterReason.ShouldBe("TentativasEsgotadas");
        worker.Notificador.TentativasPara(evento.EventoId).ShouldBe(4, "MaxTentativas = 4 na WorkerFactory");
    }

    [Fact]
    public async Task CorpoQueNaoEPedidoCriado_VaiParaADlqComoContratoInvalido()
    {
        await ambiente.DrenarAsync();
        await using var worker = new WorkerFactory(ambiente).Iniciar();
        var id = Guid.NewGuid();

        await ambiente.PublicarAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes("{ isso não é json")) { MessageId = id.ToString() });

        await Esperas.Eventualmente(async () => await ambiente.NaDlqAsync(id) is not null, "mensagem na DLQ");
        (await ambiente.NaDlqAsync(id))!.DeadLetterReason.ShouldBe("ContratoInvalido");
    }

    [Fact]
    public async Task ReprocessarDlq_DepoisDaCorrecao_NotificaUmaVez()
    {
        await ambiente.DrenarAsync();
        await using var worker = new WorkerFactory(ambiente);
        var provedorForaDoAr = true;
        worker.Notificador.Comportamento = (_, _, _) =>
            provedorForaDoAr ? throw new FalhaPermanenteException("conta do provedor suspensa") : Task.CompletedTask;
        worker.Iniciar();
        var evento = Novo.Pedido();
        await ambiente.PublicarAsync(Novo.Mensagem(evento));
        await Esperas.Eventualmente(async () => await ambiente.NaDlqAsync(evento.EventoId) is not null, "mensagem na DLQ");

        provedorForaDoAr = false; // alguém pagou a conta do provedor
        var devolvidas = await worker.Services.GetRequiredService<ReprocessadorDeDlq>().ReprocessarAsync();

        devolvidas.ShouldBeGreaterThanOrEqualTo(1);
        await Esperas.Eventualmente(() => worker.Notificador.EnviadasPara(evento.EventoId) == 1, "notificação após reprocessar");
        (await ambiente.NaDlqAsync(evento.EventoId)).ShouldBeNull();
    }
}
