namespace F1M03.Async.Tests;

// Passo 1 — Task.WhenAll com limite de concorrência e tratamento de exceções.
public class ProcessadorEmLoteTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessarAsync_RetornaResultadosNaOrdemDaEntrada()
    {
        var itens = Enumerable.Range(1, 20).ToArray();

        var resultado = await ProcessadorEmLote.ProcessarAsync(
            itens,
            async (n, ct) =>
            {
                // Itens pares "demoram" mais (cedem a thread) para embaralhar a ordem de conclusão.
                if (n % 2 == 0) await Task.Yield();
                return n * 10;
            },
            maxConcorrencia: 4,
            Ct);

        resultado.ShouldBe(itens.Select(n => n * 10).ToArray());
    }

    [Fact]
    public async Task ProcessarAsync_NuncaPassaDoLimiteDeConcorrencia()
    {
        const int limite = 3;
        var emExecucao = 0;
        var iniciados = 0;
        var limiteAtingido = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var liberar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tarefa = ProcessadorEmLote.ProcessarAsync(
            Enumerable.Range(1, 10),
            async (n, ct) =>
            {
                Interlocked.Increment(ref iniciados);
                if (Interlocked.Increment(ref emExecucao) == limite) limiteAtingido.TrySetResult();
                await liberar.Task.WaitAsync(ct);
                Interlocked.Decrement(ref emExecucao);
                return n;
            },
            limite,
            Ct);

        await limiteAtingido.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct);
        await Task.Delay(50, Ct); // dá chance a um 4º item "furar" o limite, se a implementação estiver errada
        Volatile.Read(ref iniciados).ShouldBe(limite);

        liberar.SetResult();
        (await tarefa).Length.ShouldBe(10);
    }

    [Fact]
    public async Task ProcessarAsync_ConcorrenciaInvalida_Lanca()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            ProcessadorEmLote.ProcessarAsync([1], (n, ct) => Task.FromResult(n), 0, Ct));
    }

    [Fact]
    public async Task ProcessarAsync_VariasFalhas_LancaAggregateComTodas()
    {
        var ex = await Should.ThrowAsync<AggregateException>(() =>
            ProcessadorEmLote.ProcessarAsync(
                Enumerable.Range(1, 6),
                async (n, ct) =>
                {
                    await Task.Yield();
                    return n % 3 == 0 ? throw new InvalidOperationException($"Falhou {n}") : n;
                },
                maxConcorrencia: 2,
                Ct));

        ex.InnerExceptions.Count.ShouldBe(2);
        ex.InnerExceptions.Select(e => e.Message).OrderBy(m => m).ShouldBe(["Falhou 3", "Falhou 6"]);
    }

    [Fact]
    public async Task ProcessarAsync_RepassaOTokenEPropagaCancelamento()
    {
        using var cts = new CancellationTokenSource();
        var tokensRecebidos = new System.Collections.Concurrent.ConcurrentBag<CancellationToken>();

        var tarefa = ProcessadorEmLote.ProcessarAsync(
            Enumerable.Range(1, 5),
            async (n, ct) =>
            {
                tokensRecebidos.Add(ct);
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return n;
            },
            maxConcorrencia: 2,
            cts.Token);

        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(tarefa);
        tokensRecebidos.ShouldAllBe(t => t == cts.Token);
    }
}
