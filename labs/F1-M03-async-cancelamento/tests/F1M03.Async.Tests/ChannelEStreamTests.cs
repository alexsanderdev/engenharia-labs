using System.Collections.Concurrent;

namespace F1M03.Async.Tests;

// Passo 4 — Channel<T> produtor/consumidor.
public class PipelineComChannelTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessarAsync_ProcessaCadaItemExatamenteUmaVez()
    {
        var processados = new ConcurrentBag<int>();

        var resultado = await PipelineComChannel.ProcessarAsync(
            Enumerable.Range(1, 100),
            async (n, ct) =>
            {
                processados.Add(n);
                await Task.Yield();
                return n * 2;
            },
            capacidade: 5,
            consumidores: 3,
            Ct);

        resultado.Order().ShouldBe(Enumerable.Range(1, 100).Select(n => n * 2));
        processados.Count.ShouldBe(100);
    }

    [Fact]
    public async Task ProcessarAsync_ParametrosInvalidos_Lancam()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            PipelineComChannel.ProcessarAsync([1], (n, ct) => ValueTask.FromResult(n), 0, 1, Ct));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            PipelineComChannel.ProcessarAsync([1], (n, ct) => ValueTask.FromResult(n), 1, 0, Ct));
    }

    [Fact]
    public async Task ProcessarAsync_EtapaFalha_PropagaExcecaoSemTravarOProdutor()
    {
        var tarefa = PipelineComChannel.ProcessarAsync<int, int>(
            Enumerable.Range(1, 1_000),
            async (n, ct) =>
            {
                await Task.Yield();
                return n == 3 ? throw new InvalidOperationException("Falha no item 3") : n;
            },
            capacidade: 1,
            consumidores: 1,
            Ct);

        // Se o produtor ficar preso no canal cheio, o WaitAsync estoura e o teste falha (em vez de travar).
        var ex = await Should.ThrowAsync<InvalidOperationException>(tarefa.WaitAsync(TimeSpan.FromSeconds(5), Ct));
        ex.Message.ShouldBe("Falha no item 3");
    }

    [Fact]
    public async Task ProcessarAsync_ChamadorCancela_LancaOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        var tarefa = PipelineComChannel.ProcessarAsync(
            Enumerable.Range(1, 10),
            async (n, ct) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return n;
            },
            capacidade: 2,
            consumidores: 2,
            cts.Token);

        await cts.CancelAsync();

        var ex = await Should.ThrowAsync<Exception>(tarefa.WaitAsync(TimeSpan.FromSeconds(5), Ct));
        ex.ShouldBeAssignableTo<OperationCanceledException>();
    }
}

// Passo 5 — IAsyncEnumerable com cancelamento.
public class LeitorPaginadoTests
{
    private static readonly string[][] Paginas = [["p1", "p2"], ["p3", "p4"], ["p5"]];

    private static Func<int, CancellationToken, Task<IReadOnlyList<string>>> Fonte(List<int> paginasBuscadas, List<CancellationToken>? tokens = null) =>
        async (pagina, ct) =>
        {
            paginasBuscadas.Add(pagina);
            tokens?.Add(ct);
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            return pagina <= Paginas.Length ? Paginas[pagina - 1] : [];
        };

    [Fact]
    public async Task LerTodosAsync_EntregaTodosOsItensEParaNaPaginaVazia()
    {
        var buscadas = new List<int>();
        var itens = new List<string>();

        await foreach (var item in LeitorPaginado.LerTodosAsync(Fonte(buscadas), TestContext.Current.CancellationToken))
            itens.Add(item);

        itens.ShouldBe(["p1", "p2", "p3", "p4", "p5"]);
        buscadas.ShouldBe([1, 2, 3, 4]);
    }

    [Fact]
    public async Task LerTodosAsync_ConsumidorParaNoMeio_NaoBuscaPaginasExtras()
    {
        var buscadas = new List<int>();
        var itens = new List<string>();

        await foreach (var item in LeitorPaginado.LerTodosAsync(Fonte(buscadas), TestContext.Current.CancellationToken))
        {
            itens.Add(item);
            if (itens.Count == 3) break;
        }

        itens.ShouldBe(["p1", "p2", "p3"]);
        buscadas.ShouldBe([1, 2]);
    }

    [Fact]
    public async Task LerTodosAsync_WithCancellation_RepassaTokenParaAFonte()
    {
        using var cts = new CancellationTokenSource();
        var buscadas = new List<int>();
        var tokens = new List<CancellationToken>();
        var itens = new List<string>();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in LeitorPaginado.LerTodosAsync(Fonte(buscadas, tokens)).WithCancellation(cts.Token))
            {
                itens.Add(item);
                if (itens.Count == 2) await cts.CancelAsync();
            }
        });

        itens.ShouldBe(["p1", "p2"]);
        tokens.ShouldNotBeEmpty();
        tokens.ShouldAllBe(t => t == cts.Token);
    }
}
