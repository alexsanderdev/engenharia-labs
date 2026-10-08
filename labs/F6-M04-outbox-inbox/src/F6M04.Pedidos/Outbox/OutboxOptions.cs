namespace F6M04.Pedidos.Outbox;

/// <summary>PRONTO. Parâmetros do <see cref="OutboxProcessor"/>.</summary>
public sealed class OutboxOptions
{
    /// <summary>De quanto em quanto tempo o processor procura mensagens pendentes (polling).</summary>
    public TimeSpan Intervalo { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Quantas mensagens cada rodada reserva e publica (uma transação por lote).</summary>
    public int TamanhoDoLote { get; set; } = 50;

    /// <summary>
    /// Depois de tantas tentativas, a mensagem é considerada ENVENENADA: para de ser tentada e fica na tabela
    /// (com <see cref="OutboxMessage.UltimoErro"/>) para alguém investigar. Ela também segura as mensagens
    /// posteriores do mesmo agregado (ordem estrita).
    /// </summary>
    public int MaximoDeTentativas { get; set; } = 5;

    /// <summary>Espera depois da 1ª falha; dobra a cada falha, até <see cref="EsperaMaxima"/>.</summary>
    public TimeSpan EsperaInicial { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan EsperaMaxima { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Por quanto tempo mensagens JÁ publicadas ficam na tabela antes da limpeza (auditoria/diagnóstico).</summary>
    public TimeSpan RetencaoDasProcessadas { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Backoff exponencial: 1ª falha → <see cref="EsperaInicial"/>, 2ª → 2×, 3ª → 4×... limitado a <see cref="EsperaMaxima"/>.</summary>
    public TimeSpan CalcularEspera(int tentativas)
    {
        if (tentativas <= 0) return TimeSpan.Zero;
        var fator = Math.Pow(2, Math.Min(tentativas - 1, 30));
        var espera = TimeSpan.FromTicks((long)Math.Min(EsperaInicial.Ticks * fator, EsperaMaxima.Ticks));
        return espera;
    }
}
