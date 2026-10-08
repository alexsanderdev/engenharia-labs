using System.Diagnostics;

namespace F6M04.Tests.Infra;

/// <summary>
/// PRONTO. Espera ATIVA com limite: verifica a condição a cada poucos milissegundos e falha com uma mensagem clara
/// se ela não ficar verdadeira a tempo. Nunca "Task.Delay(2000) e torcer": o teste segue assim que a condição vale.
/// </summary>
public static class Esperas
{
    public static readonly TimeSpan TempoMaximo = TimeSpan.FromSeconds(10);

    public static async Task Eventualmente(Func<Task<bool>> condicao, string descricao, TimeSpan? tempoMaximo = null)
    {
        var limite = tempoMaximo ?? TempoMaximo;
        var cronometro = Stopwatch.StartNew();
        while (true)
        {
            if (await condicao()) return;
            if (cronometro.Elapsed > limite)
                throw new TimeoutException($"Não aconteceu em {limite.TotalSeconds:0} s: {descricao}");
            await Task.Delay(20);
        }
    }

    public static Task Eventualmente(Func<bool> condicao, string descricao, TimeSpan? tempoMaximo = null) =>
        Eventualmente(() => Task.FromResult(condicao()), descricao, tempoMaximo);
}

/// <summary>
/// PRONTO. Um "ponto de parada" para pausar uma execução num lugar exato e intercalar duas execuções de forma
/// DETERMINÍSTICA (o mesmo do lab de Transações e Locks).
/// </summary>
public sealed class PontoDeParada(string nome) : IDisposable
{
    public static readonly TimeSpan TempoMaximo = TimeSpan.FromSeconds(20);

    private readonly TaskCompletionSource _chegou = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _liberado = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _chamadas;

    public int Chamadas => Volatile.Read(ref _chamadas);

    /// <summary>Na primeira chegada, avisa e espera <see cref="Liberar"/>; depois, passa direto.</summary>
    public async Task Gancho()
    {
        Interlocked.Increment(ref _chamadas);
        _chegou.TrySetResult();
        await _liberado.Task.WaitAsync(TempoMaximo);
    }

    /// <summary>Espera a execução chegar ao ponto; se ela terminar antes (ex.: lançou exceção), falha na hora com a causa.</summary>
    public async Task EsperarChegadaAsync(Task execucao)
    {
        Task primeiro;
        try
        {
            primeiro = await Task.WhenAny(_chegou.Task, execucao).WaitAsync(TempoMaximo);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"A execução {nome} não chegou ao ponto de parada em {TempoMaximo.TotalSeconds} s.");
        }

        if (primeiro == _chegou.Task) return;

        await execucao; // propaga a exceção (ex.: NotImplementedException do TODO)
        throw new InvalidOperationException($"A execução {nome} terminou sem passar pelo ponto de parada.");
    }

    public void Liberar() => _liberado.TrySetResult();

    public void Dispose() => Liberar();
}
