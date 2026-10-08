using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace F6M06.Worker.Saude;

/// <summary>
/// Health check de um worker baseado em batimento ("heartbeat"). O processo estar de pé NÃO prova
/// que o loop está rodando: um worker pode estar travado num I/O sem timeout, ou morto com o host
/// vivo (<c>BackgroundServiceExceptionBehavior.Ignore</c>).
/// </summary>
public sealed class WorkerHealthCheck(MonitorDeWorkers monitor, TimeProvider relogio, string worker, TimeSpan tolerancia)
    : IHealthCheck
{
    /// <summary>
    /// Regras, nesta ordem:
    /// <list type="number">
    /// <item>falha fatal registrada → <see cref="HealthStatus.Unhealthy"/> (o worker morreu);</item>
    /// <item>nunca bateu → Unhealthy ("ainda não executou nenhum ciclo");</item>
    /// <item>último batimento mais velho que <paramref name="tolerancia"/> (agora − batimento &gt; tolerância) → Unhealthy (travado);</item>
    /// <item>falhas consecutivas &gt; 0 → <see cref="HealthStatus.Degraded"/> (vivo, mas falhando);</item>
    /// <item>senão → Healthy.</item>
    /// </list>
    /// Inclua em <c>data</c> as chaves <c>"ultimoBatimento"</c> (DateTimeOffset ou null) e <c>"falhasConsecutivas"</c> (int).
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "TODO (Passo 5): falha fatal → Unhealthy; nunca bateu → Unhealthy; batimento mais velho que a tolerância → Unhealthy; " +
            "falhas consecutivas → Degraded; senão Healthy. Data: ultimoBatimento e falhasConsecutivas");
    }
}
