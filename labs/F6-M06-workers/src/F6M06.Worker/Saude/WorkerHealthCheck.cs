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
        var estado = monitor.Obter(worker);
        var dados = new Dictionary<string, object>
        {
            ["ultimoBatimento"] = estado.UltimoBatimento?.ToString("O") ?? "nunca",
            ["falhasConsecutivas"] = estado.FalhasConsecutivas,
        };

        if (estado.FalhaFatal is not null)
            return Task.FromResult(HealthCheckResult.Unhealthy($"{worker} morreu.", estado.FalhaFatal, dados));

        if (estado.UltimoBatimento is not { } batimento)
            return Task.FromResult(HealthCheckResult.Unhealthy($"{worker} ainda não executou nenhum ciclo.", data: dados));

        var idade = relogio.GetUtcNow() - batimento;
        if (idade > tolerancia)
            return Task.FromResult(HealthCheckResult.Unhealthy($"{worker} sem batimento há {idade} (travado?).", data: dados));

        if (estado.FalhasConsecutivas > 0)
            return Task.FromResult(HealthCheckResult.Degraded($"{worker} falhou {estado.FalhasConsecutivas} ciclo(s) seguidos.", data: dados));

        return Task.FromResult(HealthCheckResult.Healthy($"{worker} ok.", dados));
    }
}
