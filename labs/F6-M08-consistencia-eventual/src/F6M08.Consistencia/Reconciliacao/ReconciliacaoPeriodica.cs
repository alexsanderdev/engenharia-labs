using F6M08.Consistencia.Consistencia;
using Microsoft.Extensions.Options;

namespace F6M08.Consistencia.Reconciliacao;

/// <summary>
/// Roda o <see cref="Reconciliador"/> a cada <see cref="ConsistenciaOptions.IntervaloDaReconciliacao"/>.
/// <c>PeriodicTimer</c> com <see cref="TimeProvider"/>: nos testes, só "tica" quando o relógio falso anda.
/// Pronto: leia, não altere.
/// </summary>
public sealed class ReconciliacaoPeriodica(
    Reconciliador reconciliador,
    IOptions<ConsistenciaOptions> opcoes,
    TimeProvider relogio,
    ILogger<ReconciliacaoPeriodica> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(opcoes.Value.IntervaloDaReconciliacao, relogio);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var relatorio = reconciliador.Reconciliar();
                    if (relatorio.Corrigidas.Count > 0)
                    {
                        // Divergência corrigida é sintoma: alguém perdeu evento ou o projetor tem bug. Vira alerta, não só log.
                        logger.LogWarning("Reconciliação corrigiu {Quantidade} divergência(s): {Divergencias}",
                            relatorio.Corrigidas.Count, relatorio.Corrigidas);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Falha na reconciliação.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // desligamento normal
        }
    }
}
