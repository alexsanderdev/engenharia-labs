namespace F3M03.Transacoes.Tests.Infra;

/// <summary>
/// PRONTO. Um "ponto de parada" para pausar uma transação num lugar exato e intercalar duas
/// execuções de forma DETERMINÍSTICA (nada de Thread.Sleep e torcer).
/// </summary>
/// <example>
/// <code>
/// using var pausaA = new PontoDeParada("A");
/// var a = servico.ReservarAsync(..., entreLeituraEEscrita: pausaA.Gancho);
/// await pausaA.EsperarChegadaAsync(a);  // A leu e está parada com a transação aberta
/// ...                                     // faça B acontecer aqui
/// pausaA.Liberar();                       // A continua e grava
/// await a;
/// </code>
/// </example>
public sealed class PontoDeParada(string nome) : IDisposable
{
    /// <summary>Tempo máximo que qualquer espera da orquestração aguarda antes de falhar o teste.</summary>
    public static readonly TimeSpan TempoMaximo = TimeSpan.FromSeconds(20);

    private readonly TaskCompletionSource _chegou = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _liberado = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _chamadas;

    /// <summary>Quantas vezes o código passou pelo ponto (útil para contar tentativas de retry).</summary>
    public int Chamadas => Volatile.Read(ref _chamadas);

    /// <summary>Tarefa que completa na PRIMEIRA vez que o código chega ao ponto.</summary>
    public Task Chegou => _chegou.Task;

    /// <summary>
    /// O gancho a ser passado para o código de produção. Na primeira chegada, avisa e espera
    /// <see cref="Liberar"/>; depois de liberado, as próximas chamadas passam direto.
    /// </summary>
    public Func<Task> Gancho => async () =>
    {
        Interlocked.Increment(ref _chamadas);
        _chegou.TrySetResult();
        await _liberado.Task.WaitAsync(TempoMaximo);
    };

    /// <summary>
    /// Espera o código chegar ao ponto. Se <paramref name="execucao"/> terminar antes (lançou exceção,
    /// ou terminou sem passar pelo gancho), falha na hora com a causa, em vez de esperar o tempo máximo.
    /// </summary>
    public async Task EsperarChegadaAsync(Task execucao)
    {
        Task primeiro;
        try
        {
            primeiro = await Task.WhenAny(_chegou.Task, execucao).WaitAsync(TempoMaximo);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException(
                $"A execução {nome} não chegou ao ponto de parada em {TempoMaximo.TotalSeconds} s (ficou bloqueada antes dele?).");
        }

        if (primeiro == _chegou.Task) return;

        await execucao; // propaga a exceção (ex.: NotImplementedException do TODO)
        throw new InvalidOperationException(
            $"A execução {nome} terminou sem passar pelo ponto de parada: o gancho foi chamado no lugar certo?");
    }

    /// <summary>Deixa a execução parada seguir.</summary>
    public void Liberar() => _liberado.TrySetResult();

    /// <summary>Sempre libera ao sair do teste, para nenhuma execução ficar pendurada.</summary>
    public void Dispose() => Liberar();
}
