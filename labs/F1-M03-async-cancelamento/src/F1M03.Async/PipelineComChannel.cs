using System.Collections.Concurrent;
using System.Threading.Channels;

namespace F1M03.Async;

/// <summary>
/// Produtor/consumidor com <see cref="Channel{T}"/>: um produtor escreve os pedidos num canal LIMITADO
/// (backpressure) e N consumidores processam em paralelo (ex.: enviar notificação de pedido criado).
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
    public static async Task<IReadOnlyList<TSaida>> ProcessarAsync<TEntrada, TSaida>(
        IEnumerable<TEntrada> entrada,
        Func<TEntrada, CancellationToken, ValueTask<TSaida>> etapa,
        int capacidade,
        int consumidores,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        ArgumentNullException.ThrowIfNull(etapa);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacidade);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(consumidores);

        var canal = Channel.CreateBounded<TEntrada>(new BoundedChannelOptions(capacidade)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = consumidores == 1
        });
        var resultados = new ConcurrentQueue<TSaida>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        async Task Produzir()
        {
            try
            {
                foreach (var item in entrada)
                    await canal.Writer.WriteAsync(item, cts.Token).ConfigureAwait(false);
            }
            finally
            {
                canal.Writer.TryComplete();
            }
        }

        async Task Consumir()
        {
            try
            {
                await foreach (var item in canal.Reader.ReadAllAsync(cts.Token).ConfigureAwait(false))
                    resultados.Enqueue(await etapa(item, cts.Token).ConfigureAwait(false));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Sem isto, o produtor pode ficar preso para sempre em WriteAsync com o canal cheio.
                await cts.CancelAsync().ConfigureAwait(false);
                throw;
            }
        }

        Task[] tarefas = [Produzir(), .. Enumerable.Range(0, consumidores).Select(_ => Consumir())];
        await Task.WhenAll(tarefas).ConfigureAwait(false);
        return [.. resultados];
    }
}
