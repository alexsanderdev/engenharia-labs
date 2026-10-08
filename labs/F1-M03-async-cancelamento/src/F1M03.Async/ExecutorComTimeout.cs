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
    public Task<T> ExecutarAsync<T>(
        Func<CancellationToken, Task<T>> operacao,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        // TODO: transforme este método em async.
        // 1. new CancellationTokenSource(timeout, _tempo)  -> o prazo "anda" no relógio do TimeProvider.
        // 2. CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token).
        // 3. catch (OperationCanceledException) when (foi o timeout e NÃO o chamador) -> TimeoutException.
        _ = _tempo;
        throw new NotImplementedException("TODO: implemente ExecutarAsync com CTS + TimeProvider + linked token");
    }
}
