using System.Collections.Concurrent;
using F6M06.Worker.Notificacoes;
using F6M06.Worker.Pedidos;
using Microsoft.Extensions.Time.Testing;

namespace F6M06.Worker.Tests.Infra;

/// <summary>
/// PRONTO. FakeTimeProvider que conta os timers criados: o teste espera o worker criar o
/// <c>PeriodicTimer</c> antes de avançar o relógio (senão o Advance acontece "antes" do timer existir).
/// </summary>
public sealed class RelogioDeTeste() : FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero))
{
    private int _timers;

    public int TimersCriados => Volatile.Read(ref _timers);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        Interlocked.Increment(ref _timers);
        return timer;
    }
}

/// <summary>PRONTO. Polling com timeout curto — nunca um <c>Task.Delay</c> fixo "para dar tempo".</summary>
public static class Eventualmente
{
    public static async Task Ate(Func<bool> condicao, string descricao, TimeSpan? timeout = null)
    {
        var limite = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (!condicao())
        {
            if (DateTime.UtcNow > limite)
                throw new TimeoutException($"Eventualmente: {descricao}");
            await Task.Delay(5);
        }
    }

    public static async Task Ate(Func<Task<bool>> condicao, string descricao, TimeSpan? timeout = null)
    {
        var limite = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (!await condicao())
        {
            if (DateTime.UtcNow > limite)
                throw new TimeoutException($"Eventualmente: {descricao}");
            await Task.Delay(5);
        }
    }
}

/// <summary>
/// PRONTO. Controle compartilhado (singleton) do <see cref="RepositorioDeTeste"/>: programa falhas e
/// travamentos e registra cada instância (escopo) usada.
/// </summary>
public sealed class ControleDoRepositorio
{
    private int _falhasProgramadas;
    private TaskCompletionSource? _trava;
    private TaskCompletionSource? _travaEmUso;

    public ConcurrentQueue<Guid> InstanciasUsadas { get; } = new();

    public int Consultas => InstanciasUsadas.Count;

    /// <summary>As próximas <paramref name="vezes"/> consultas lançam (int.MaxValue = sempre).</summary>
    public void FalharNasProximas(int vezes) => Volatile.Write(ref _falhasProgramadas, vezes);

    /// <summary>A próxima consulta fica presa até <see cref="Destravar"/> (simula I/O pendurado).</summary>
    public void TravarProximaConsulta() =>
        _trava = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Destravar()
    {
        _trava?.TrySetResult();
        _travaEmUso?.TrySetResult();
    }

    internal async Task AntesDeConsultarAsync(Guid instancia)
    {
        InstanciasUsadas.Enqueue(instancia);
        if (_trava is { } trava)
        {
            _trava = null;
            _travaEmUso = trava;
            await trava.Task;
        }

        if (Volatile.Read(ref _falhasProgramadas) > 0)
        {
            Interlocked.Decrement(ref _falhasProgramadas);
            throw new InvalidOperationException("Banco fora do ar (simulado)");
        }
    }
}

/// <summary>PRONTO. Repositório scoped de teste: delega ao real e obedece ao <see cref="ControleDoRepositorio"/>.</summary>
public sealed class RepositorioDeTeste(BancoEmMemoria banco, ControleDoRepositorio controle) : IRepositorioDePedidos
{
    private readonly Guid _instancia = Guid.NewGuid();
    private readonly RepositorioDePedidosEmMemoria _real = new(banco);

    public async Task<IReadOnlyList<Pedido>> ListarCriadosAteAsync(DateTimeOffset limite, CancellationToken ct)
    {
        await controle.AntesDeConsultarAsync(_instancia);
        return await _real.ListarCriadosAteAsync(limite, ct);
    }

    public Task SalvarAsync(CancellationToken ct) => _real.SalvarAsync(ct);
}

/// <summary>
/// PRONTO. Enviador de notificações controlável: cada envio espera o teste liberar (ou não),
/// mede a concorrência e registra quem terminou, falhou ou foi interrompido.
/// </summary>
public sealed class EnviadorFake : IEnviadorDeNotificacoes
{
    private readonly TaskCompletionSource _liberar = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _emAndamento;
    private int _maximoSimultaneo;

    /// <summary>Se false, cada envio só termina depois de <see cref="LiberarTodos"/> (ou do cancelamento).</summary>
    public bool Imediato { get; set; } = true;

    /// <summary>Ids que devem falhar com exceção.</summary>
    public HashSet<Guid> Falhar { get; } = [];

    public ConcurrentQueue<Guid> Iniciadas { get; } = new();
    public ConcurrentQueue<Guid> Concluidas { get; } = new();
    public ConcurrentQueue<Guid> Interrompidas { get; } = new();

    public int EmAndamento => Volatile.Read(ref _emAndamento);
    public int MaximoSimultaneo => Volatile.Read(ref _maximoSimultaneo);

    public void LiberarTodos() => _liberar.TrySetResult();

    public async Task EnviarAsync(Notificacao notificacao, CancellationToken ct)
    {
        Iniciadas.Enqueue(notificacao.Id);
        var atual = Interlocked.Increment(ref _emAndamento);
        int maximo;
        while (atual > (maximo = Volatile.Read(ref _maximoSimultaneo))
               && Interlocked.CompareExchange(ref _maximoSimultaneo, atual, maximo) != maximo)
        {
        }

        try
        {
            if (Falhar.Contains(notificacao.Id))
                throw new InvalidOperationException("SMTP recusou (simulado)");
            if (!Imediato)
                await _liberar.Task.WaitAsync(ct);
            Concluidas.Enqueue(notificacao.Id);
        }
        catch (OperationCanceledException)
        {
            Interrompidas.Enqueue(notificacao.Id);
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _emAndamento);
        }
    }
}
