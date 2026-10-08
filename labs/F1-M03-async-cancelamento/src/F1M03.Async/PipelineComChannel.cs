namespace F1M03.Async;

// Dica: using System.Threading.Channels; e System.Collections.Concurrent para juntar os resultados.

/// <summary>
/// Produtor/consumidor com <see cref="System.Threading.Channels.Channel{T}"/>: um produtor escreve os pedidos
/// num canal LIMITADO (backpressure) e N consumidores processam em paralelo
/// (ex.: enviar notificação de pedido criado).
/// </summary>
public static class PipelineComChannel
{
    /// <summary>
    /// Processa todos os itens de <paramref name="entrada"/> com <paramref name="consumidores"/> consumidores
    /// lendo de um canal com capacidade <paramref name="capacidade"/>. Retorna os resultados (ordem NÃO garantida).
    /// <para>Regras:</para>
    /// <list type="bullet">
    /// <item>Capacidade ou consumidores &lt;= 0 lançam <see cref="ArgumentOutOfRangeException"/>.</item>
    /// <item>Cada item é processado exatamente uma vez.</item>
    /// <item>Se uma etapa falhar, a exceção original sobe e TODO o pipeline para (produtor e demais
    /// consumidores são cancelados) — sem travar, mesmo com o canal cheio.</item>
    /// <item>Cancelamento do chamador gera <see cref="OperationCanceledException"/>.</item>
    /// </list>
    /// </summary>
    public static Task<IReadOnlyList<TSaida>> ProcessarAsync<TEntrada, TSaida>(
        IEnumerable<TEntrada> entrada,
        Func<TEntrada, CancellationToken, ValueTask<TSaida>> etapa,
        int capacidade,
        int consumidores,
        CancellationToken cancellationToken = default)
    {
        // TODO: transforme este método em async.
        // 1. Channel.CreateBounded<TEntrada>(new BoundedChannelOptions(capacidade) { FullMode = Wait }).
        // 2. Um CTS linkado ao token do chamador.
        // 3. Produtor: WriteAsync de cada item; no finally, Writer.TryComplete().
        // 4. N consumidores: await foreach em Reader.ReadAllAsync(token); se a etapa falhar, cancele o CTS e relance.
        // 5. await Task.WhenAll(produtor + consumidores) e retorne os resultados.
        throw new NotImplementedException("TODO: implemente o pipeline produtor/consumidor com Channel<T>");
    }
}
