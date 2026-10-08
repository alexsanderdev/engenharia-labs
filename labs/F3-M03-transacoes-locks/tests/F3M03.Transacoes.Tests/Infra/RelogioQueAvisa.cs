using Microsoft.Extensions.Time.Testing;

namespace F3M03.Transacoes.Tests.Infra;

/// <summary>
/// PRONTO. <see cref="FakeTimeProvider"/> que avisa quando alguém cria um timer (o que
/// <c>Task.Delay(atraso, relogio)</c> faz). Assim o teste só avança o relógio DEPOIS que o código
/// começou a esperar, sem corrida entre "avançar o tempo" e "registrar a espera".
/// </summary>
public sealed class RelogioQueAvisa : FakeTimeProvider, IDisposable
{
    private readonly SemaphoreSlim _timersCriados = new(0);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        _timersCriados.Release();
        return timer;
    }

    /// <summary>Espera o próximo timer ser criado (falha em 5 s se o código não chegar a esperar).</summary>
    public async Task EsperarAlguemComecarAEsperarAsync()
    {
        if (!await _timersCriados.WaitAsync(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("O código não começou nenhuma espera no relógio (Task.Delay(atraso, relogio, ct)).");
    }

    public void Dispose() => _timersCriados.Dispose();
}
