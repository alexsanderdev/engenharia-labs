using Microsoft.Extensions.Options;
using MP3.Notificacoes.Worker.Configuracao;

namespace MP3.Notificacoes.Worker.Mensageria;

/// <summary>
/// Backoff exponencial com teto e jitter: tentativa 1 falhou → espera base; 2 → 2×base; 3 → 4×base...
/// O jitter espalha os retries de muitas mensagens que falharam juntas (evita a "manada" batendo no provedor ao mesmo tempo).
/// </summary>
public sealed class PoliticaDeRetry(IOptions<NotificacoesOptions> opcoes, Func<double>? aleatorio = null)
{
    private readonly Func<double> aleatorio = aleatorio ?? Random.Shared.NextDouble;

    public int MaxTentativas => opcoes.Value.Retry.MaxTentativas;

    /// <summary>Atraso antes da PRÓXIMA tentativa, dado o número da tentativa que acabou de falhar (1, 2, 3...).</summary>
    public TimeSpan AtrasoApos(int tentativaQueFalhou)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tentativaQueFalhou, 1);
        var r = opcoes.Value.Retry;
        var exponencial = r.AtrasoBase.TotalMilliseconds * Math.Pow(2, tentativaQueFalhou - 1);
        var limitado = Math.Min(exponencial, r.AtrasoMaximo.TotalMilliseconds);
        var fator = 1 + (r.Jitter * ((aleatorio() * 2) - 1)); // [1 - jitter, 1 + jitter]
        return TimeSpan.FromMilliseconds(limitado * fator);
    }

    public bool DeveTentarDeNovo(int tentativaQueFalhou) => tentativaQueFalhou < MaxTentativas;
}
