using System.Diagnostics;
using System.Threading.Channels;
using F6M06.Worker.Notificacoes;
using F6M06.Worker.Tests.Infra;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace F6M06.Worker.Tests;

/// <summary>Passos 7 a 9 — Channel limitado, N leitores e desligamento gracioso.</summary>
public class NotificacoesTests
{
    private static Notificacao Nova(int n) => new(Guid.NewGuid(), Guid.NewGuid(), $"Pedido {n} recebido");

    [Fact]
    public async Task Fila_Cheia_TentarEnfileirarRecusa_EEnfileirarAsyncEsperaVaga()
    {
        var fila = new FilaDeNotificacoes(Options.Create(new NotificacoesOptions { Capacidade = 2 }));

        fila.TentarEnfileirar(Nova(1)).ShouldBeTrue();
        fila.TentarEnfileirar(Nova(2)).ShouldBeTrue();
        fila.TentarEnfileirar(Nova(3)).ShouldBeFalse("capacidade 2: a fila não cresce sem limite");

        var produtorLento = fila.EnfileirarAsync(Nova(4), TestContext.Current.CancellationToken).AsTask();
        produtorLento.IsCompleted.ShouldBeFalse("backpressure: o produtor espera vaga");

        fila.Leitor.TryRead(out _).ShouldBeTrue();
        await produtorLento.WaitAsync(TimeSpan.FromSeconds(5));
        fila.Pendentes.ShouldBe(2);

        fila.Encerrar();
        fila.TentarEnfileirar(Nova(5)).ShouldBeFalse("encerrada não aceita itens novos");
        await Should.ThrowAsync<ChannelClosedException>(() => fila.EnfileirarAsync(Nova(6)).AsTask());
    }

    [Fact]
    public async Task Processador_ComTresLeitores_ProcessaEmParaleloSemPassarDoLimite()
    {
        await using var ambiente = Ambiente.Criar(new() { ["Notificacoes:Leitores"] = "3" }, enviadorImediato: false);
        await ambiente.IniciarAsync();

        for (var i = 1; i <= 5; i++)
            await ambiente.Fila.EnfileirarAsync(Nova(i));

        await Eventualmente.Ate(() => ambiente.Enviador.EmAndamento == 3, "3 leitores deveriam trabalhar em paralelo");
        ambiente.Enviador.LiberarTodos();
        await Eventualmente.Ate(() => ambiente.Enviador.Concluidas.Count == 5, "as 5 notificações deveriam ser enviadas");
        ambiente.Enviador.MaximoSimultaneo.ShouldBe(3);
    }

    [Fact]
    public async Task Processador_FalhaEmUmaNotificacao_NaoDerrubaOLeitor()
    {
        await using var ambiente = Ambiente.Criar();
        var ruim = Nova(1);
        var boa = Nova(2);
        ambiente.Enviador.Falhar.Add(ruim.Id);
        await ambiente.IniciarAsync();

        await ambiente.Fila.EnfileirarAsync(ruim);
        await ambiente.Fila.EnfileirarAsync(boa);

        await Eventualmente.Ate(() => ambiente.Enviador.Concluidas.Contains(boa.Id), "o leitor morreu na notificação ruim?");
        ambiente.Logs.GetSnapshot().ShouldContain(r => r.Id.Id == 6103 && r.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Parada_TerminaOItemEmAndamento_NaoComecaNovos_ENaoAceitaMais()
    {
        await using var ambiente = Ambiente.Criar(enviadorImediato: false); // 1 leitor
        await ambiente.IniciarAsync();
        var emAndamento = Nova(1);
        await ambiente.Fila.EnfileirarAsync(emAndamento);
        await ambiente.Fila.EnfileirarAsync(Nova(2));
        await ambiente.Fila.EnfileirarAsync(Nova(3));
        await Eventualmente.Ate(() => ambiente.Enviador.EmAndamento == 1, "o leitor deveria pegar o 1º item");

        var parada = ambiente.Host.StopAsync(); // SIGTERM
        await Eventualmente.Ate(() => !ambiente.Fila.TentarEnfileirar(Nova(4)), "parando: a fila deveria recusar itens novos");
        parada.IsCompleted.ShouldBeFalse("o host espera o item em andamento");

        ambiente.Enviador.LiberarTodos();
        await parada.WaitAsync(TimeSpan.FromSeconds(5));

        ambiente.Enviador.Concluidas.ToArray().ShouldBe([emAndamento.Id], "terminou o que estava fazendo…");
        ambiente.Enviador.Iniciadas.Count.ShouldBe(1, "…e não começou os outros");
        ambiente.Enviador.Interrompidas.ShouldBeEmpty();
        ambiente.Fila.Pendentes.ShouldBe(2);
        var aviso = ambiente.Logs.GetSnapshot().Single(r => r.Id.Id == 6104);
        aviso.Level.ShouldBe(LogLevel.Warning);
        aviso.GetStructuredStateValue("Quantidade").ShouldBe("2");
    }

    [Fact]
    public async Task Parada_ItemQueNaoTerminaNoShutdownTimeout_EhInterrompido_EOHostNaoTrava()
    {
        await using var ambiente = Ambiente.Criar(
            enviadorImediato: false,
            hostOptions: o => o.ShutdownTimeout = TimeSpan.FromMilliseconds(300));
        await ambiente.IniciarAsync();
        var lenta = Nova(1);
        await ambiente.Fila.EnfileirarAsync(lenta);
        await Eventualmente.Ate(() => ambiente.Enviador.EmAndamento == 1, "o leitor deveria pegar o item");

        var cronometro = Stopwatch.StartNew();
        await ambiente.Host.StopAsync().WaitAsync(TimeSpan.FromSeconds(5)); // nunca liberamos o envio

        cronometro.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(4));
        await Eventualmente.Ate(() => ambiente.Enviador.Interrompidas.Contains(lenta.Id),
            "estourado o ShutdownTimeout, o envio deveria receber cancelamento");
        await Eventualmente.Ate(() => ambiente.Logs.GetSnapshot().Any(r => r.Id.Id == 6102), "a interrupção deveria ser logada");
        ambiente.Enviador.Concluidas.ShouldBeEmpty();
    }
}
