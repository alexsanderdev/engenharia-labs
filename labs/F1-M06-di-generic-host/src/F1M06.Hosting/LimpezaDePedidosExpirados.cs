using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace F1M06.Hosting;

/// <summary>
/// Worker que cancela pedidos expirados periodicamente.
/// Hosted services são SINGLETON: para usar um serviço Scoped (<see cref="IServicoDePedidos"/>),
/// o worker cria um escopo novo a cada ciclo com <see cref="IServiceScopeFactory"/>.
/// </summary>
public sealed partial class LimpezaDePedidosExpirados(
    IServiceScopeFactory scopeFactory,
    TimeProvider tempo,
    IOptions<OpcoesPedidos> opcoes,
    ILogger<LimpezaDePedidosExpirados> logger) : BackgroundService
{
    /// <summary>
    /// Executa um ciclo IMEDIATAMENTE ao iniciar e depois a cada <see cref="OpcoesPedidos.IntervaloLimpeza"/>,
    /// usando <c>new PeriodicTimer(intervalo, tempo)</c> (o TimeProvider permite testar com FakeTimeProvider).
    /// Termina sem erro quando <paramref name="stoppingToken"/> é cancelado.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(opcoes.Value.IntervaloLimpeza, tempo);

        try
        {
            do
            {
                await ExecutarCicloAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Parada normal do host.
        }
    }

    /// <summary>
    /// Um ciclo: cria um escopo (<c>CreateAsyncScope</c>), resolve <see cref="IServicoDePedidos"/> e chama
    /// <see cref="IServicoDePedidos.CancelarExpiradosAsync"/>. Uma exceção em um ciclo é LOGADA e NÃO derruba
    /// o worker (no .NET 6+ uma exceção não tratada em BackgroundService para o host inteiro).
    /// </summary>
    private async Task ExecutarCicloAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var escopo = scopeFactory.CreateAsyncScope();
            var servico = escopo.ServiceProvider.GetRequiredService<IServicoDePedidos>();
            var cancelados = await servico.CancelarExpiradosAsync(stoppingToken);
            LogCicloConcluido(cancelados);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCicloFalhou(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ciclo de limpeza concluído: {Cancelados} pedidos cancelados")]
    private partial void LogCicloConcluido(int cancelados);

    [LoggerMessage(Level = LogLevel.Error, Message = "Ciclo de limpeza falhou; tentando de novo no próximo intervalo")]
    private partial void LogCicloFalhou(Exception ex);
}
