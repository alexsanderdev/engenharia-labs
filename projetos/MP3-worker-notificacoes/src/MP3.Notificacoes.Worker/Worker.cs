namespace MP3.Notificacoes.Worker;

/// <summary>
/// Ponto de partida. Substitua por um consumidor de verdade (veja a "Missão" no README):
/// <list type="number">
/// <item>Consumir <c>PedidoCriado</c> da fila <c>notificacoes-pedidos</c> com <c>ServiceBusProcessor</c>
/// (sem auto-complete: você decide completar, abandonar ou mandar para a DLQ).</item>
/// <item>Inbox no SQL Server: a mesma mensagem entregue 2× gera 1 notificação.</item>
/// <item>Retry com backoff para falha transitória; DLQ para falha permanente; comando para reprocessar a DLQ.</item>
/// <item>Notificadores plugáveis (Strategy): console, SMTP fake, SMS fake.</item>
/// <item>Graceful shutdown, health checks e (opcional) trace continuando o da API.</item>
/// </list>
/// </summary>
public sealed partial class Worker(ILogger<Worker> logger, IConfiguration configuracao) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var fila = configuracao["ServiceBus:Fila"];
        LogIniciado(logger, fila);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogParando(logger);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "MP3 Worker de Notificações iniciado. Fila configurada: {Fila}. Nada é consumido ainda.")]
    private static partial void LogIniciado(ILogger logger, string? fila);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker parando.")]
    private static partial void LogParando(ILogger logger);
}
