using Microsoft.Extensions.Time.Testing;

namespace F1M03.Async.Tests;

// Passo 2 — Timeout com CancellationTokenSource + TimeProvider (testado com FakeTimeProvider).
public class ExecutorComTimeoutTests
{
    private readonly FakeTimeProvider _tempo = new();

    private static async Task<int> EsperarParaSempre(CancellationToken ct)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        return 0;
    }

    [Fact]
    public async Task ExecutarAsync_TerminaAntesDoPrazo_RetornaResultado()
    {
        var executor = new ExecutorComTimeout(_tempo);

        var tarefa = executor.ExecutarAsync(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), _tempo, ct);
            return 42;
        }, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        tarefa.IsCompleted.ShouldBeFalse();
        _tempo.Advance(TimeSpan.FromSeconds(2));

        (await tarefa).ShouldBe(42);
    }

    [Fact]
    public async Task ExecutarAsync_PrazoEstourado_LancaTimeoutException()
    {
        var executor = new ExecutorComTimeout(_tempo);

        var tarefa = executor.ExecutarAsync(EsperarParaSempre, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        _tempo.Advance(TimeSpan.FromSeconds(4.9));
        tarefa.IsCompleted.ShouldBeFalse();
        _tempo.Advance(TimeSpan.FromSeconds(0.1));

        var ex = await Should.ThrowAsync<TimeoutException>(tarefa);
        ex.InnerException.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecutarAsync_ChamadorCancela_PropagaOperationCanceledENaoTimeout()
    {
        var executor = new ExecutorComTimeout(_tempo);
        using var cts = new CancellationTokenSource();

        var tarefa = executor.ExecutarAsync(EsperarParaSempre, TimeSpan.FromSeconds(5), cts.Token);
        await cts.CancelAsync();

        var ex = await Should.ThrowAsync<Exception>(tarefa);
        ex.ShouldBeAssignableTo<OperationCanceledException>();
    }
}

// Passo 3 — ValueTask: caminho síncrono quando o preço está em cache.
public class CacheDePrecosTests
{
    [Fact]
    public async Task ObterPrecoAsync_SegundaChamada_CompletaSincronaSemChamarAFonte()
    {
        var chamadas = 0;
        var cache = new CacheDePrecos(async (id, ct) =>
        {
            Interlocked.Increment(ref chamadas);
            await Task.Yield();
            return 30m;
        });
        var id = Guid.NewGuid();

        (await cache.ObterPrecoAsync(id, TestContext.Current.CancellationToken)).ShouldBe(30m);
        var segunda = cache.ObterPrecoAsync(id, TestContext.Current.CancellationToken);

        segunda.IsCompletedSuccessfully.ShouldBeTrue();
        (await segunda).ShouldBe(30m);
        chamadas.ShouldBe(1);
    }

    [Fact]
    public async Task ObterPrecoAsync_RepassaTokenENaoGuardaFalhas()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken recebido = default;
        var falhar = true;
        var cache = new CacheDePrecos((id, ct) =>
        {
            recebido = ct;
            return falhar ? Task.FromException<decimal>(new HttpRequestException("fora do ar")) : Task.FromResult(9.9m);
        });
        var id = Guid.NewGuid();

        await Should.ThrowAsync<HttpRequestException>(cache.ObterPrecoAsync(id, cts.Token).AsTask());
        recebido.ShouldBe(cts.Token);

        falhar = false;
        (await cache.ObterPrecoAsync(id, cts.Token)).ShouldBe(9.9m);
    }
}
