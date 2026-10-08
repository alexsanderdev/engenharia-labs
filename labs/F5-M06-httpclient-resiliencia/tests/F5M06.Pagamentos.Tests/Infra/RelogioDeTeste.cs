using System.Collections.Concurrent;
using Microsoft.Extensions.Time.Testing;

namespace F5M06.Pagamentos.Tests.Infra;

/// <summary>
/// FakeTimeProvider que ANOTA cada timer agendado (Task.Delay do retry, CancellationTokenSource dos
/// timeouts, break do circuito). Assim o teste espera o pipeline "parar para esperar" antes de avançar
/// o relógio: nada de sleep, nada de corrida entre o teste e o Polly.
/// (Infra pronta: você não precisa alterar este arquivo.)
/// </summary>
public sealed class RelogioDeTeste() : FakeTimeProvider(new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero))
{
    private readonly ConcurrentQueue<TimeSpan> _agendamentos = new();

    /// <summary>Todos os "dueTime" já agendados, na ordem.</summary>
    public IReadOnlyList<TimeSpan> Agendamentos => [.. _agendamentos];

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        Anotar(dueTime);
        return new TimerAnotado(timer, this);
    }

    /// <summary>
    /// Espera (tempo real, no máximo 5 s) até existir o <paramref name="ocorrencia"/>-ésimo agendamento
    /// que satisfaz <paramref name="filtro"/> e devolve o valor dele.
    /// </summary>
    public async Task<TimeSpan> AguardarAgendamentoAsync(Func<TimeSpan, bool> filtro, int ocorrencia = 1)
    {
        var limite = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < limite)
        {
            var encontrados = _agendamentos.Where(filtro).ToList();
            if (encontrados.Count >= ocorrencia)
            {
                return encontrados[ocorrencia - 1];
            }

            await Task.Delay(2);
        }

        throw new TimeoutException(
            $"O pipeline não agendou o {ocorrencia}º timer esperado. Agendados: [{string.Join(", ", _agendamentos)}]");
    }

    /// <summary>Espera o agendamento de exatamente <paramref name="dueTime"/> (n-ésima ocorrência).</summary>
    public Task<TimeSpan> AguardarAgendamentoAsync(TimeSpan dueTime, int ocorrencia = 1) =>
        AguardarAgendamentoAsync(t => t == dueTime, ocorrencia);

    private void Anotar(TimeSpan dueTime)
    {
        if (dueTime > TimeSpan.Zero && dueTime != Timeout.InfiniteTimeSpan)
        {
            _agendamentos.Enqueue(dueTime);
        }
    }

    private sealed class TimerAnotado(ITimer interno, RelogioDeTeste relogio) : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            relogio.Anotar(dueTime);
            return interno.Change(dueTime, period);
        }

        public void Dispose() => interno.Dispose();

        public ValueTask DisposeAsync() => interno.DisposeAsync();
    }
}
