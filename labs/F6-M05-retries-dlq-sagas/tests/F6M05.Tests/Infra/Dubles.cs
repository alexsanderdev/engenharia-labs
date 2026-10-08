using System.Collections.Concurrent;
using F6M05.Sagas.Mensageria;
using F6M05.Sagas.Saga;

namespace F6M05.Tests.Infra;

/// <summary>
/// PRONTA. Manipulador de teste para o consumidor: registra cada chamada e, segundo um roteiro,
/// lança (ou não) uma exceção. <c>falhar(n, mensagem)</c> recebe o número da chamada (1, 2, ...).
/// </summary>
public sealed class ManipuladorRoteirizado(Func<int, MensagemRecebida, Exception?> falhar)
{
    private int _chamadas;

    public ConcurrentQueue<MensagemRecebida> Chamadas { get; } = new();
    public ConcurrentQueue<MensagemRecebida> Sucessos { get; } = new();
    public int TotalDeChamadas => Volatile.Read(ref _chamadas);

    public Task ProcessarAsync(MensagemRecebida mensagem, CancellationToken ct)
    {
        Chamadas.Enqueue(mensagem);
        var n = Interlocked.Increment(ref _chamadas);
        var erro = falhar(n, mensagem);
        if (erro is not null) throw erro;
        Sucessos.Enqueue(mensagem);
        return Task.CompletedTask;
    }

    public static ManipuladorRoteirizado SempreOk() => new((_, _) => null);
}

/// <summary>PRONTA. Publicador de comandos em memória (testes do orquestrador sem broker).</summary>
public sealed class PublicadorEmMemoria : IPublicadorDeComandos
{
    public ConcurrentQueue<ComandoSaga> Publicados { get; } = new();

    public Task PublicarAsync(ComandoSaga comando, CancellationToken ct = default)
    {
        Publicados.Enqueue(comando);
        return Task.CompletedTask;
    }

    public IReadOnlyList<ComandoSaga> DoPedido(Guid pedidoId) => [.. Publicados.Where(c => c.PedidoId == pedidoId)];
}

/// <summary>
/// PRONTA. Repositório que segura a PRIMEIRA leitura até o teste liberar: permite montar, sem
/// sleep, a corrida "A leu a versão N; B gravou N+1; A tenta gravar sobre N".
/// </summary>
public sealed class RepositorioComPausa(IRepositorioDeSagas real) : IRepositorioDeSagas
{
    private int _leituras;
    private int _conflitos;

    /// <summary>Sinalizado quando a primeira leitura terminou (e ficou parada).</summary>
    public TaskCompletionSource PrimeiraLeituraFeita { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>O teste completa para a primeira leitura seguir.</summary>
    public TaskCompletionSource Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int Leituras => Volatile.Read(ref _leituras);
    public int Conflitos => Volatile.Read(ref _conflitos);

    public async Task<SagaPedido?> ObterAsync(Guid pedidoId, CancellationToken ct = default)
    {
        var saga = await real.ObterAsync(pedidoId, ct);
        if (Interlocked.Increment(ref _leituras) == 1)
        {
            PrimeiraLeituraFeita.TrySetResult();
            await Liberar.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        }
        return saga;
    }

    public async Task InserirAsync(SagaPedido saga, string messageId, CancellationToken ct = default)
    {
        try { await real.InserirAsync(saga, messageId, ct); }
        catch (ConflitoDeConcorrenciaException) { Interlocked.Increment(ref _conflitos); throw; }
    }

    public async Task AtualizarAsync(SagaPedido saga, string messageId, CancellationToken ct = default)
    {
        try { await real.AtualizarAsync(saga, messageId, ct); }
        catch (ConflitoDeConcorrenciaException) { Interlocked.Increment(ref _conflitos); throw; }
    }

    public Task<IReadOnlyList<Guid>> ListarComPrazoVencidoAsync(DateTimeOffset agora, int maximo = 100, CancellationToken ct = default) =>
        real.ListarComPrazoVencidoAsync(agora, maximo, ct);
}

/// <summary>PRONTA. Fábrica de mensagens de teste com ids legíveis.</summary>
public static class Msg
{
    public static readonly DateTimeOffset Inicio = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public static PedidoCriado PedidoCriado(Guid pedidoId, decimal valor = 150m) =>
        new($"pedido-criado-{pedidoId:N}", pedidoId, Guid.NewGuid(), valor,
            [new ItemDoPedido(Guid.NewGuid(), 2), new ItemDoPedido(Guid.NewGuid(), 1)]);

    public static EstoqueReservado EstoqueReservado(Guid pedidoId, string sufixo = "1") => new($"estoque-reservado-{sufixo}-{pedidoId:N}", pedidoId);
    public static EstoqueIndisponivel EstoqueIndisponivel(Guid pedidoId) => new($"estoque-indisponivel-{pedidoId:N}", pedidoId, "sem saldo");
    public static PagamentoAutorizado PagamentoAutorizado(Guid pedidoId) => new($"pagamento-autorizado-{pedidoId:N}", pedidoId, $"aut-{pedidoId:N}");
    public static PagamentoRecusado PagamentoRecusado(Guid pedidoId) => new($"pagamento-recusado-{pedidoId:N}", pedidoId, "cartão sem limite");
    public static EstoqueLiberado EstoqueLiberado(Guid pedidoId) => new($"estoque-liberado-{pedidoId:N}", pedidoId);
    public static PrazoDoPagamentoExpirado PrazoExpirado(Guid pedidoId) => new($"prazo-{pedidoId:N}", pedidoId);
}
