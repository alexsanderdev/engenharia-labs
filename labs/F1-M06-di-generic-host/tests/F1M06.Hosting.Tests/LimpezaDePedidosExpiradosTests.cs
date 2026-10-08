using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace F1M06.Hosting.Tests;

/// <summary>Registra cada ciclo do worker (singleton compartilhado entre os escopos).</summary>
public sealed class RegistroDeCiclos
{
    private readonly SemaphoreSlim _sinal = new(0);

    public ConcurrentQueue<Guid> Escopos { get; } = new();
    public int FalharNasPrimeiras { get; set; }

    public void Registrar(Guid escopo)
    {
        Escopos.Enqueue(escopo);
        _sinal.Release();
    }

    /// <summary>Espera até <paramref name="total"/> ciclos terem acontecido (ou estoura o timeout).</summary>
    public async Task AguardarAsync(int total)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (Escopos.Count < total)
            await _sinal.WaitAsync(cts.Token);
    }
}

/// <summary>Fake Scoped de <see cref="IServicoDePedidos"/>.</summary>
public sealed class ServicoDePedidosFake(IContextoDaOperacao contexto, RegistroDeCiclos registro) : IServicoDePedidos
{
    public Task<int> CancelarExpiradosAsync(CancellationToken cancellationToken)
    {
        var numero = registro.Escopos.Count + 1;
        registro.Registrar(contexto.Id);
        if (numero <= registro.FalharNasPrimeiras)
            throw new InvalidOperationException("Banco fora do ar (simulado)");
        return Task.FromResult(0);
    }
}

public class LimpezaDePedidosExpiradosTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    private static IHost CriarHost(FakeTimeProvider relogio, RegistroDeCiclos registro) =>
        OrderFlowHost.Criar(
            new Dictionary<string, string?> { ["Pedidos:IntervaloLimpeza"] = "00:10:00" },
            s =>
            {
                s.AddSingleton<TimeProvider>(relogio);
                s.AddSingleton(registro);
                s.AddScoped<IServicoDePedidos, ServicoDePedidosFake>(); // o último registro vence
            });

    [Fact]
    public async Task AoIniciar_ExecutaUmCicloImediatamente()
    {
        var registro = new RegistroDeCiclos();
        using var host = CriarHost(new FakeTimeProvider(Inicio), registro);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await registro.AguardarAsync(1);
        await host.StopAsync(TestContext.Current.CancellationToken);

        registro.Escopos.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AposOIntervalo_ExecutaDeNovoEmUmEscopoNovo()
    {
        var relogio = new FakeTimeProvider(Inicio);
        var registro = new RegistroDeCiclos();
        using var host = CriarHost(relogio, registro);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await registro.AguardarAsync(1);

        relogio.Advance(TimeSpan.FromMinutes(10));
        await registro.AguardarAsync(2);
        await host.StopAsync(TestContext.Current.CancellationToken);

        registro.Escopos.Distinct().Count().ShouldBe(2, "cada ciclo precisa de um escopo (e um contexto) novo");
    }

    [Fact]
    public async Task ExcecaoEmUmCiclo_NaoDerrubaOWorker()
    {
        var relogio = new FakeTimeProvider(Inicio);
        var registro = new RegistroDeCiclos { FalharNasPrimeiras = 1 };
        using var host = CriarHost(relogio, registro);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await registro.AguardarAsync(1);

        relogio.Advance(TimeSpan.FromMinutes(10));
        await registro.AguardarAsync(2); // estouraria o timeout se o worker tivesse morrido
        await host.StopAsync(TestContext.Current.CancellationToken);

        registro.Escopos.Count.ShouldBe(2);
    }

    [Fact]
    public async Task StopAsync_EncerraOWorkerSemErro()
    {
        var registro = new RegistroDeCiclos();
        using var host = CriarHost(new FakeTimeProvider(Inicio), registro);
        var worker = host.Services.GetServices<IHostedService>().OfType<LimpezaDePedidosExpirados>().Single();

        await host.StartAsync(TestContext.Current.CancellationToken);
        await registro.AguardarAsync(1);
        await host.StopAsync(TestContext.Current.CancellationToken);

        worker.ExecuteTask.ShouldNotBeNull().IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task ServicoDePedidosReal_CancelaSoOsExpirados()
    {
        var relogio = new FakeTimeProvider(Inicio);
        using var host = OrderFlowHost.Criar(configurarServicos: s => s.AddSingleton<TimeProvider>(relogio));
        var repositorio = host.Services.GetRequiredService<IRepositorioPedidos>();
        repositorio.Adicionar(new PedidoPendente(Guid.NewGuid(), Inicio.AddMinutes(-31))); // expirado
        repositorio.Adicionar(new PedidoPendente(Guid.NewGuid(), Inicio.AddMinutes(-5)));  // ainda vale

        await using var escopo = host.Services.CreateAsyncScope();
        var cancelados = await escopo.ServiceProvider.GetRequiredService<IServicoDePedidos>()
            .CancelarExpiradosAsync(TestContext.Current.CancellationToken);

        cancelados.ShouldBe(1);
        repositorio.Todos().Count(p => p.Cancelado).ShouldBe(1);
    }
}
