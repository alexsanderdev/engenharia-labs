using System.Threading.Channels;

namespace F6M08.Consistencia.Mensageria;

/// <summary>
/// "Broker" em memória para tornar visível — e determinística — a janela de inconsistência.
/// <para>
/// Cada mensagem publicada só fica disponível no <see cref="Leitor"/> depois de um atraso medido no
/// <see cref="TimeProvider"/> injetado. Nos testes, com <c>FakeTimeProvider</c>, nada chega até o teste
/// mandar o relógio andar (<c>Advance</c>): a janela "entre gravar e aparecer na leitura" vira algo que
/// você controla, em vez de um sleep que às vezes passa e às vezes não.
/// </para>
/// <para>
/// Também injeta as falhas que um broker real produz: atraso diferente por mensagem (entrega fora de ordem),
/// entrega duplicada (at-least-once) e perda (bug de publicação, mensagem expirada, fila apagada).
/// </para>
/// </summary>
public sealed class FilaComAtraso<T> where T : class
{
    private readonly TimeProvider relogio;
    private readonly Channel<T> canal = Channel.CreateUnbounded<T>();
    private readonly Lock trava = new();
    private readonly Dictionary<long, ITimer> emTransito = [];
    private readonly List<T> historico = [];
    private long sequencia;

    public FilaComAtraso(TimeProvider relogio, TimeSpan atrasoPadrao)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(atrasoPadrao, TimeSpan.Zero);
        this.relogio = relogio;
        AtrasoPadrao = atrasoPadrao;
    }

    /// <summary>Atraso aplicado a toda mensagem sem regra específica.</summary>
    public TimeSpan AtrasoPadrao { get; set; }

    /// <summary>Regra de atraso por mensagem (devolva <c>null</c> para usar o padrão). Use para entregar fora de ordem.</summary>
    public Func<T, TimeSpan?>? AtrasoPorMensagem { get; set; }

    /// <summary>Mensagens para as quais devolver <c>true</c> são entregues DUAS vezes (at-least-once).</summary>
    public Func<T, bool>? Duplicar { get; set; }

    /// <summary>Mensagens para as quais devolver <c>true</c> são PERDIDAS (nunca chegam ao consumidor).</summary>
    public Func<T, bool>? Perder { get; set; }

    /// <summary>Lado do consumidor.</summary>
    public ChannelReader<T> Leitor => canal.Reader;

    /// <summary>Quantas entregas ainda estão "no fio" (agendadas e não disponíveis).</summary>
    public int EmTransito
    {
        get { lock (trava) return emTransito.Count; }
    }

    /// <summary>Tudo o que foi publicado, na ordem de publicação (inclusive o que foi perdido) — o "log" para rebuild.</summary>
    public IReadOnlyList<T> Historico
    {
        get { lock (trava) return [.. historico]; }
    }

    public void Publicar(T mensagem)
    {
        ArgumentNullException.ThrowIfNull(mensagem);
        lock (trava)
        {
            historico.Add(mensagem);
            if (Perder?.Invoke(mensagem) == true) return;

            var atraso = AtrasoPorMensagem?.Invoke(mensagem) ?? AtrasoPadrao;
            Agendar(mensagem, atraso);
            if (Duplicar?.Invoke(mensagem) == true) Agendar(mensagem, atraso);
        }
    }

    private void Agendar(T mensagem, TimeSpan atraso)
    {
        if (atraso <= TimeSpan.Zero)
        {
            canal.Writer.TryWrite(mensagem);
            return;
        }

        var id = ++sequencia;
        // O callback roda quando o relógio (real ou falso) passa do atraso.
        var timer = relogio.CreateTimer(_ => Entregar(id, mensagem), null, atraso, Timeout.InfiniteTimeSpan);
        emTransito[id] = timer;
    }

    private void Entregar(long id, T mensagem)
    {
        lock (trava)
        {
            if (!emTransito.Remove(id, out var timer)) return;
            timer.Dispose();
        }
        canal.Writer.TryWrite(mensagem);
    }
}
