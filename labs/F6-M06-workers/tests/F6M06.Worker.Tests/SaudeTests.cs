using F6M06.Worker.Expiracao;
using F6M06.Worker.Saude;
using F6M06.Worker.Tests.Infra;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace F6M06.Worker.Tests;

/// <summary>Passos 5 e 6 — health check por batimento e o comportamento do host diante de exceções.</summary>
public class SaudeTests
{
    private static Task<HealthCheckResult> Checar(MonitorDeWorkers monitor, TimeProvider relogio) =>
        new WorkerHealthCheck(monitor, relogio, "w", TimeSpan.FromMinutes(3))
            .CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task HealthCheck_RegrasDoBatimento()
    {
        var relogio = new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));
        var monitor = new MonitorDeWorkers(relogio);

        (await Checar(monitor, relogio)).Status.ShouldBe(HealthStatus.Unhealthy, "nunca bateu");

        monitor.RegistrarCiclo("w", sucesso: true);
        var saudavel = await Checar(monitor, relogio);
        saudavel.Status.ShouldBe(HealthStatus.Healthy);
        saudavel.Data.ShouldContainKey("ultimoBatimento");

        monitor.RegistrarCiclo("w", sucesso: false);
        var degradado = await Checar(monitor, relogio);
        degradado.Status.ShouldBe(HealthStatus.Degraded, "vivo, mas o último ciclo falhou");
        degradado.Data["falhasConsecutivas"].ShouldBe(1);

        relogio.Advance(TimeSpan.FromMinutes(3));
        (await Checar(monitor, relogio)).Status.ShouldBe(HealthStatus.Degraded, "3 min ainda está dentro da tolerância");
        relogio.Advance(TimeSpan.FromSeconds(1));
        (await Checar(monitor, relogio)).Status.ShouldBe(HealthStatus.Unhealthy, "batimento velho = travado");

        monitor.RegistrarCiclo("w", sucesso: true);
        monitor.RegistrarFalhaFatal("w", new InvalidOperationException("morreu"));
        (await Checar(monitor, relogio)).Status.ShouldBe(HealthStatus.Unhealthy, "falha fatal vence batimento recente");
    }

    [Fact]
    public async Task HealthCheck_NoHost_WorkerTravadoEmIO_FicaUnhealthy_EVoltaAoDestravar()
    {
        await using var ambiente = Ambiente.Criar();
        await ambiente.IniciarAsync();
        await Eventualmente.Ate(async () => (await ambiente.SaudeAsync()).Status == HealthStatus.Healthy, "worker saudável após o 1º ciclo");

        ambiente.Repositorio.TravarProximaConsulta(); // o 2º ciclo vai pendurar num "I/O sem timeout"
        await ambiente.AvancarAsync(TimeSpan.FromMinutes(1));
        await Eventualmente.Ate(() => ambiente.Repositorio.Consultas == 2, "segundo ciclo começou");
        ambiente.Relogio.Advance(TimeSpan.FromMinutes(3));

        var relatorio = await ambiente.SaudeAsync();
        relatorio.Status.ShouldBe(HealthStatus.Unhealthy, "o processo está de pé, mas o loop não bate há 4 min");
        relatorio.Entries[ExpiracaoDePedidosWorker.Nome].Tags.ShouldContain(DependencyInjection.TagWorker);

        ambiente.Repositorio.Destravar();
        await Eventualmente.Ate(async () => (await ambiente.SaudeAsync()).Status == HealthStatus.Healthy, "destravou → bate de novo");
    }

    [Fact]
    public async Task ComBackgroundServiceExceptionBehaviorIgnore_HostSegueVivo_MasOHealthCheckDenunciaOWorkerMorto()
    {
        await using var ambiente = Ambiente.Criar(
            new() { ["Expiracao:MaxFalhasConsecutivas"] = "1" },
            hostOptions: o => o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
        ambiente.Repositorio.FalharNasProximas(int.MaxValue);

        await ambiente.IniciarAsync();

        var worker = ambiente.Services.GetServicesOfType<ExpiracaoDePedidosWorker>();
        await Eventualmente.Ate(() => worker.ExecuteTask?.IsFaulted == true, "o worker deveria morrer na 1ª falha (limite 1)");
        ambiente.Ciclo.ApplicationStopping.IsCancellationRequested.ShouldBeFalse("Ignore: o host nem percebe");
        (await ambiente.SaudeAsync()).Status.ShouldBe(HealthStatus.Unhealthy, "só o health check salva você aqui");
    }
}

internal static class ServiceProviderExtensions
{
    public static T GetServicesOfType<T>(this IServiceProvider services) where T : class =>
        ((IEnumerable<IHostedService>)services.GetService(typeof(IEnumerable<IHostedService>))!).OfType<T>().Single();
}
