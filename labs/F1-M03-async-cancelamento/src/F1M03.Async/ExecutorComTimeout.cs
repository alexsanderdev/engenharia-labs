namespace F1M03.Async;

/// <summary>
/// Executa uma operação assíncrona com prazo máximo, usando <see cref="TimeProvider"/>
/// para que o tempo seja controlável em testes (FakeTimeProvider).
/// </summary>
public sealed class ExecutorComTimeout(TimeProvider tempo)
{
    private readonly TimeProvider _tempo = tempo ?? throw new ArgumentNullException(nameof(tempo));

    /// <summary>
    /// Executa <paramref name="operacao"/> passando um token que é cancelado quando:
    /// (a) o chamador cancela <paramref name="cancellationToken"/>; ou (b) o <paramref name="timeout"/> expira
    /// segundo o <see cref="TimeProvider"/> injetado.
    /// <para>Se o cancelamento veio do TIMEOUT, lança <see cref="TimeoutException"/>
    /// (com a <see cref="OperationCanceledException"/> original como InnerException).</para>
    /// <para>Se veio do CHAMADOR, deixa a <see cref="OperationCanceledException"/> subir.</para>
    /// Os <see cref="CancellationTokenSource"/> criados devem ser descartados (using).
    /// </summary>
    public async Task<T> ExecutarAsync<T>(
        Func<CancellationToken, Task<T>> operacao,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operacao);

        using var timeoutCts = new CancellationTokenSource(timeout, _tempo);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await operacao(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
            when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"A operação excedeu o limite de {timeout.TotalSeconds:0.##}s.", ex);
        }
    }
}
