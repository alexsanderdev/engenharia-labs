using F6M06.Worker.Expiracao;
using F6M06.Worker.Pedidos;
using F6M06.Worker.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace F6M06.Worker.Tests;

/// <summary>Passos 1 a 4 — composição, caso de uso e o job periódico com PeriodicTimer + TimeProvider.</summary>
public class ExpiracaoTests
{
    [Fact]
    public async Task Composicao_HostDeDevelopmentValidaEscopos_ERegistraOsDoisWorkers()
    {
        // Development liga ValidateScopes + ValidateOnBuild: um worker (singleton) que recebesse
        // um serviço scoped no construtor quebraria AQUI, no Build.
        await using var ambiente = Ambiente.Criar();

        var hosted = ambiente.Services.GetServices<IHostedService>().ToList();
        hosted.OfType<ExpiracaoDePedidosWorker>().ShouldHaveSingleItem();
        hosted.OfType<F6M06.Worker.Notificacoes.ProcessadorDeNotificacoesWorker>().ShouldHaveSingleItem();
        ambiente.Services.GetRequiredService<TimeProvider>().ShouldBeSameAs(ambiente.Relogio);
    }

    [Fact]
    public async Task ServicoDeExpiracao_CancelaSoOsPedidosCriadosAlemDoPrazo()
    {
        await using var ambiente = Ambiente.Criar();
        var vencido = ambiente.NovoPedido(TimeSpan.FromMinutes(31));
        var noPrazo = ambiente.NovoPedido(TimeSpan.FromMinutes(5));
        var confirmado = ambiente.NovoPedido(TimeSpan.FromHours(2));
        confirmado.Confirmar();

        await using var escopo = ambiente.Services.CreateAsyncScope();
        var cancelados = await escopo.ServiceProvider.GetRequiredService<ServicoDeExpiracao>()
            .ExpirarAsync(TestContext.Current.CancellationToken);

        cancelados.ShouldBe(1);
        vencido.Status.ShouldBe(StatusPedido.Cancelled);
        vencido.MotivoCancelamento.ShouldBe(ServicoDeExpiracao.Motivo);
        noPrazo.Status.ShouldBe(StatusPedido.Created);
        confirmado.Status.ShouldBe(StatusPedido.Confirmed, "Confirmed não cancela");
    }

    [Fact]
    public async Task Worker_AoIniciar_RodaUmCicloJa_ELogaAQuantidadeDeFormaEstruturada()
    {
        await using var ambiente = Ambiente.Criar();
        var vencido = ambiente.NovoPedido(TimeSpan.FromHours(1));

        await ambiente.IniciarAsync();

        await Eventualmente.Ate(() => vencido.Status == StatusPedido.Cancelled, "o primeiro ciclo deveria rodar ao iniciar");
        await Eventualmente.Ate(() => ambiente.Logs.GetSnapshot().Any(r => r.Id.Id == 6001), "o ciclo deveria ser logado");
        var log = ambiente.Logs.GetSnapshot().First(r => r.Id.Id == 6001);
        log.Level.ShouldBe(LogLevel.Information);
        log.GetStructuredStateValue("Quantidade").ShouldBe("1");
        log.GetStructuredStateValue("DuracaoMs").ShouldNotBeNull();
        ambiente.Monitor.Obter(ExpiracaoDePedidosWorker.Nome).UltimoBatimento.ShouldBe(ambiente.Relogio.GetUtcNow());
    }

    [Fact]
    public async Task Worker_ACadaIntervalo_RodaDeNovoEmUmEscopoNovo()
    {
        await using var ambiente = Ambiente.Criar();
        await ambiente.IniciarAsync();
        await Eventualmente.Ate(() => ambiente.Repositorio.Consultas == 1, "primeiro ciclo");
        var vaiVencer = ambiente.NovoPedido(TimeSpan.FromMinutes(29.5)); // vence daqui a 30 s

        await ambiente.AvancarAsync(TimeSpan.FromMinutes(1));

        await Eventualmente.Ate(() => vaiVencer.Status == StatusPedido.Cancelled, "o segundo ciclo deveria expirar o pedido");
        ambiente.Repositorio.InstanciasUsadas.Distinct().Count().ShouldBe(2, "cada ciclo precisa de um escopo (e um repositório) novo");
    }

    [Fact]
    public async Task Worker_FalhaIsoladaNumCiclo_LogaErro_EOProximoCicloFunciona()
    {
        await using var ambiente = Ambiente.Criar();
        ambiente.Repositorio.FalharNasProximas(1);
        await ambiente.IniciarAsync();
        await Eventualmente.Ate(() => ambiente.Logs.GetSnapshot().Any(r => r.Id.Id == 6002), "a falha deveria ser logada como erro");
        var vencido = ambiente.NovoPedido(TimeSpan.FromHours(1));

        await ambiente.AvancarAsync(TimeSpan.FromMinutes(1));

        await Eventualmente.Ate(() => vencido.Status == StatusPedido.Cancelled, "o worker morreu com a primeira falha?");
        var erro = ambiente.Logs.GetSnapshot().First(r => r.Id.Id == 6002);
        erro.Level.ShouldBe(LogLevel.Error);
        erro.Exception.ShouldBeOfType<InvalidOperationException>();
        ambiente.Ciclo.ApplicationStopping.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public async Task Worker_FalhasSeguidasAteOLimite_DesisteComLogCritico_EOHostParaPorPadrao()
    {
        await using var ambiente = Ambiente.Criar(); // MaxFalhasConsecutivas = 3; padrão: StopHost
        ambiente.Repositorio.FalharNasProximas(int.MaxValue);
        await ambiente.IniciarAsync();

        await ambiente.AvancarAsync(TimeSpan.FromMinutes(1));
        await Eventualmente.Ate(() => ambiente.Repositorio.Consultas >= 2, "segundo ciclo");
        await ambiente.AvancarAsync(TimeSpan.FromMinutes(1));

        await Eventualmente.Ate(() => ambiente.Ciclo.ApplicationStopping.IsCancellationRequested,
            "com StopHost, a exceção relançada pelo worker deveria parar o host");
        ambiente.Repositorio.Consultas.ShouldBe(3);
        ambiente.Logs.GetSnapshot().ShouldContain(r => r.Id.Id == 6003 && r.Level == LogLevel.Critical);
        ambiente.Monitor.AlgumaFalhaFatal.ShouldBeTrue("o Program usa isso para sair com código ≠ 0");
    }
}
