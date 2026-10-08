using F6M06.Worker.Notificacoes;
using F6M06.Worker.Pedidos;
using F6M06.Worker.Saude;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace F6M06.Worker.Tests.Infra;

/// <summary>
/// PRONTO. Monta o HOST REAL (<c>Host.CreateApplicationBuilder</c>, ambiente Development → ValidateScopes e
/// ValidateOnBuild ligados) com <c>AddWorkersDoOrderFlow</c> e troca só as bordas: relógio, repositório,
/// enviador e logging (FakeLogger).
/// </summary>
public sealed class Ambiente : IAsyncDisposable
{
    private Ambiente(IHost host, RelogioDeTeste relogio, ControleDoRepositorio repositorio, EnviadorFake enviador)
    {
        Host = host;
        Relogio = relogio;
        Repositorio = repositorio;
        Enviador = enviador;
    }

    public IHost Host { get; }
    public RelogioDeTeste Relogio { get; }
    public ControleDoRepositorio Repositorio { get; }
    public EnviadorFake Enviador { get; }

    public IServiceProvider Services => Host.Services;
    public BancoEmMemoria Banco => Services.GetRequiredService<BancoEmMemoria>();
    public MonitorDeWorkers Monitor => Services.GetRequiredService<MonitorDeWorkers>();
    public FilaDeNotificacoes Fila => Services.GetRequiredService<FilaDeNotificacoes>();
    public FakeLogCollector Logs => Services.GetFakeLogCollector();
    public IHostApplicationLifetime Ciclo => Services.GetRequiredService<IHostApplicationLifetime>();

    /// <summary>Cria o host (não inicia).</summary>
    public static Ambiente Criar(
        Dictionary<string, string?>? configuracao = null,
        Action<HostOptions>? hostOptions = null,
        bool enviadorImediato = true)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
            DisableDefaults = false,
        });

        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Expiracao:Intervalo"] = "00:01:00",
            ["Expiracao:PrazoDePagamento"] = "00:30:00",
            ["Expiracao:ToleranciaSemBatimento"] = "00:03:00",
            ["Expiracao:MaxFalhasConsecutivas"] = "3",
            ["Notificacoes:Capacidade"] = "10",
            ["Notificacoes:Leitores"] = "1",
        });
        if (configuracao is not null)
            builder.Configuration.AddInMemoryCollection(configuracao);

        builder.Logging.ClearProviders();
        builder.Logging.AddFakeLogging();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        builder.Services.AddWorkersDoOrderFlow(builder.Configuration);

        var relogio = new RelogioDeTeste();
        var controle = new ControleDoRepositorio();
        var enviador = new EnviadorFake { Imediato = enviadorImediato };
        builder.Services.AddSingleton<TimeProvider>(relogio); // o último registro vence
        builder.Services.AddSingleton(controle);
        builder.Services.AddScoped<IRepositorioDePedidos, RepositorioDeTeste>();
        builder.Services.AddSingleton<IEnviadorDeNotificacoes>(enviador);
        builder.Services.Configure<HostOptions>(o =>
        {
            o.ShutdownTimeout = TimeSpan.FromSeconds(10);
            hostOptions?.Invoke(o);
        });

        return new Ambiente(builder.Build(), relogio, controle, enviador);
    }

    public Task IniciarAsync() => Host.StartAsync();

    /// <summary>Espera o <c>PeriodicTimer</c> do worker existir e avança o relógio.</summary>
    public async Task AvancarAsync(TimeSpan tempo)
    {
        await Eventualmente.Ate(() => Relogio.TimersCriados > 0, "o worker não criou o PeriodicTimer com o TimeProvider");
        Relogio.Advance(tempo);
    }

    public Pedido NovoPedido(TimeSpan idade)
    {
        var pedido = new Pedido(Guid.NewGuid(), Relogio.GetUtcNow() - idade);
        Banco.Adicionar(pedido);
        return pedido;
    }

    public async Task<HealthReport> SaudeAsync() =>
        await Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();

    public async ValueTask DisposeAsync()
    {
        Repositorio.Destravar();
        Enviador.LiberarTodos();
        try
        {
            await Host.StopAsync(TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException)
        {
            // teste já falhou ou o host já parou
        }

        Host.Dispose();
    }
}
