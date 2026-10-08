using System.Diagnostics;
using F6M06.Worker.Saude;
using Microsoft.Extensions.Options;

namespace F6M06.Worker.Expiracao;

/// <summary>
/// Job periódico que expira pedidos não pagos. Hosted services são SINGLETON: o serviço scoped é
/// resolvido em um escopo NOVO a cada ciclo (<see cref="IServiceScopeFactory"/>).
/// </summary>
public sealed partial class ExpiracaoDePedidosWorker(
    IServiceScopeFactory escopos,
    TimeProvider relogio,
    MonitorDeWorkers monitor,
    IOptions<ExpiracaoOptions> opcoes,
    ILogger<ExpiracaoDePedidosWorker> logger) : BackgroundService
{
    /// <summary>Nome do worker no monitor e no health check.</summary>
    public const string Nome = "expiracao-de-pedidos";

    /// <summary>
    /// Loop do job:
    /// <list type="number">
    /// <item>crie <c>new PeriodicTimer(opcoes.Value.Intervalo, relogio)</c> — o <see cref="TimeProvider"/> permite testar com FakeTimeProvider;</item>
    /// <item>rode um ciclo JÁ e depois um a cada tick: <c>do { ... } while (await timer.WaitForNextTickAsync(stoppingToken))</c>;</item>
    /// <item>cada ciclo (<see cref="ExecutarCicloAsync"/>) que dá certo zera o contador de falhas; que falha
    /// (qualquer exceção que NÃO seja o cancelamento do <paramref name="stoppingToken"/>) é logado com
    /// <see cref="LogCicloFalhou"/> e o loop continua;</item>
    /// <item>em ambos os casos, registre o batimento: <c>monitor.RegistrarCiclo(Nome, sucesso)</c>;</item>
    /// <item>ao atingir <see cref="ExpiracaoOptions.MaxFalhasConsecutivas"/> falhas seguidas: <see cref="LogDesistindo"/>
    /// (Critical), <c>monitor.RegistrarFalhaFatal(Nome, ex)</c> e RELANCE a exceção — o host decide o que fazer
    /// conforme <c>HostOptions.BackgroundServiceExceptionBehavior</c> (padrão: StopHost);</item>
    /// <item><see cref="OperationCanceledException"/> com <paramref name="stoppingToken"/> cancelado = parada normal: saia sem erro.</item>
    /// </list>
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException(
            "TODO (Passos 3 e 4): PeriodicTimer(Intervalo, relogio), ciclo já e a cada tick, batimento no monitor, " +
            "falha isolada logada e loop segue; MaxFalhasConsecutivas → LogDesistindo + RegistrarFalhaFatal + throw");
    }

    /// <summary>
    /// Um ciclo: <c>await using var escopo = escopos.CreateAsyncScope()</c>, resolva <see cref="ServicoDeExpiracao"/>,
    /// chame <see cref="ServicoDeExpiracao.ExpirarAsync"/> e logue <see cref="LogCicloConcluido"/> com a quantidade
    /// e a duração em ms (use <c>relogio.GetTimestamp()</c>/<c>GetElapsedTime</c> ou <see cref="Stopwatch"/>).
    /// </summary>
    private async Task ExecutarCicloAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
        throw new NotImplementedException(
            "TODO (Passo 3): CreateAsyncScope, resolva ServicoDeExpiracao, ExpirarAsync e LogCicloConcluido(quantidade, duraçãoMs)");
    }

    [LoggerMessage(EventId = 6001, Level = LogLevel.Information,
        Message = "Expiração concluída: {Quantidade} pedidos cancelados em {DuracaoMs} ms")]
    private partial void LogCicloConcluido(int quantidade, long duracaoMs);

    [LoggerMessage(EventId = 6002, Level = LogLevel.Error,
        Message = "Ciclo de expiração falhou ({FalhasConsecutivas}/{MaxFalhas}); tentando de novo no próximo intervalo")]
    private partial void LogCicloFalhou(Exception ex, int falhasConsecutivas, int maxFalhas);

    [LoggerMessage(EventId = 6003, Level = LogLevel.Critical,
        Message = "Worker de expiração desistiu após {FalhasConsecutivas} falhas seguidas")]
    private partial void LogDesistindo(Exception ex, int falhasConsecutivas);

    [LoggerMessage(EventId = 6004, Level = LogLevel.Information, Message = "Worker de expiração parado")]
    private partial void LogParado();
}
