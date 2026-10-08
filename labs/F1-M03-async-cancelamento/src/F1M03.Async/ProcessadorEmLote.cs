namespace F1M03.Async;

/// <summary>
/// Processa vários itens em paralelo (ex.: consultar o estoque de cada produto de um pedido),
/// limitando quantas operações rodam ao mesmo tempo.
/// </summary>
public static class ProcessadorEmLote
{
    /// <summary>
    /// Executa <paramref name="acao"/> para cada item com no máximo <paramref name="maxConcorrencia"/>
    /// execuções simultâneas e devolve os resultados NA MESMA ORDEM da entrada.
    /// <para>Regras:</para>
    /// <list type="bullet">
    /// <item><paramref name="maxConcorrencia"/> &lt;= 0 lança <see cref="ArgumentOutOfRangeException"/>.</item>
    /// <item>O <paramref name="cancellationToken"/> é repassado para cada ação.</item>
    /// <item>Se uma ou mais ações falharem, lança <see cref="AggregateException"/> com TODAS as falhas
    /// (e não só a primeira, que é o que <c>await Task.WhenAll</c> faria sozinho).</item>
    /// <item>Se só houver cancelamento, lança <see cref="OperationCanceledException"/>.</item>
    /// </list>
    /// </summary>
    public static async Task<TSaida[]> ProcessarAsync<TEntrada, TSaida>(
        IEnumerable<TEntrada> itens,
        Func<TEntrada, CancellationToken, Task<TSaida>> acao,
        int maxConcorrencia,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(acao);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcorrencia);

        using var semaforo = new SemaphoreSlim(maxConcorrencia, maxConcorrencia);

        async Task<TSaida> ExecutarUm(TEntrada item)
        {
            await semaforo.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await acao(item, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaforo.Release();
            }
        }

        var tarefas = itens.Select(ExecutarUm).ToArray();
        var todas = Task.WhenAll(tarefas);
        try
        {
            return await todas.ConfigureAwait(false);
        }
        catch when (todas.Exception is not null)
        {
            // O await só relança a PRIMEIRA exceção; a Task guarda todas.
            throw todas.Exception;
        }
    }
}
