using F4M04.Cqrs.Abstractions;
using F4M04.Cqrs.Dispatching;
using F4M04.Cqrs.Pedidos.Escrita;
using F4M04.Cqrs.Pedidos.Infra;
using F4M04.Cqrs.Pedidos.Leitura;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace F4M04.Cqrs.Tests.Infra;

/// <summary>
/// Monta o container de DI como uma aplicação faria (infra em memória + AddCqrs por varredura),
/// com relógio falso e logger falso. Cada teste cria o seu ambiente: nada é compartilhado.
/// </summary>
public sealed class Ambiente : IAsyncDisposable
{
    public static readonly DateTimeOffset Inicio = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);

    public Ambiente(bool comTiposDeTeste = false, Action<IServiceCollection>? configurar = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddFakeLogging());
        services.AddSingleton<TimeProvider>(Relogio);
        services.AddPedidosEmMemoria();
        services.AddCqrs(typeof(CriarPedido).Assembly);
        if (comTiposDeTeste) services.AddCqrs(TiposDeTeste.Pipeline);
        configurar?.Invoke(services);
        Provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public FakeTimeProvider Relogio { get; } = new(Inicio);
    public ServiceProvider Provider { get; }
    public FakeLogCollector Logs => Provider.GetRequiredService<FakeLogCollector>();
    public BancoDeEscrita Escrita => Provider.GetRequiredService<BancoDeEscrita>();
    public BancoDeLeitura Leitura => Provider.GetRequiredService<BancoDeLeitura>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Simula uma requisição: um scope novo, um dispatcher, um command.</summary>
    public async Task<TResult> EnviarAsync<TResult>(ICommand<TResult> command)
    {
        await using var scope = Provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IDispatcher>().SendAsync(command, Ct);
    }

    /// <summary>Simula uma requisição de leitura: um scope novo, um dispatcher, uma query.</summary>
    public async Task<TResult> ConsultarAsync<TResult>(IQuery<TResult> query)
    {
        await using var scope = Provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IDispatcher>().QueryAsync(query, Ct);
    }

    /// <summary>Mensagens de log na ordem em que foram escritas.</summary>
    public IReadOnlyList<string> Mensagens() => Logs.GetSnapshot().Select(r => r.Message).ToList();

    public ValueTask DisposeAsync() => Provider.DisposeAsync();
}
