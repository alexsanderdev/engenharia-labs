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
    public static Task<TSaida[]> ProcessarAsync<TEntrada, TSaida>(
        IEnumerable<TEntrada> itens,
        Func<TEntrada, CancellationToken, Task<TSaida>> acao,
        int maxConcorrencia,
        CancellationToken cancellationToken = default)
    {
        // TODO: transforme este método em async.
        // 1. Valide os argumentos.
        // 2. Use um SemaphoreSlim(maxConcorrencia) — WaitAsync(token) antes da ação, Release() no finally.
        // 3. Crie todas as tarefas, guarde o Task.WhenAll numa variável e, no catch, relance
        //    a AggregateException da Task (todas.Exception) quando ela existir.
        throw new NotImplementedException("TODO: implemente ProcessarAsync com SemaphoreSlim + Task.WhenAll");
    }
}
