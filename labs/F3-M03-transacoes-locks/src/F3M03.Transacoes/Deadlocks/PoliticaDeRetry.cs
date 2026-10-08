namespace F3M03.Transacoes.Deadlocks;

/// <summary>
/// Quantas vezes tentar e quanto esperar entre as tentativas (backoff exponencial com teto).
/// </summary>
/// <param name="MaxTentativas">Total de execuções permitidas, contando a primeira (mínimo 1).</param>
/// <param name="AtrasoBase">Espera depois da 1ª falha; dobra a cada nova falha.</param>
/// <param name="AtrasoMaximo">Teto da espera.</param>
public sealed record PoliticaDeRetry(int MaxTentativas, TimeSpan AtrasoBase, TimeSpan AtrasoMaximo)
{
    /// <summary>Padrão razoável para deadlock: 3 tentativas, 50 ms, 100 ms (teto de 1 s).</summary>
    public static PoliticaDeRetry Padrao { get; } = new(3, TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(1));

    /// <summary>
    /// Quanto esperar depois que a tentativa número <paramref name="tentativaQueFalhou"/> (1, 2, 3...)
    /// falhou: <c>AtrasoBase * 2^(tentativaQueFalhou - 1)</c>, limitado a <see cref="AtrasoMaximo"/>.
    /// Ex.: base 100 ms → 100, 200, 400, 800 ms...
    /// </summary>
    public TimeSpan CalcularAtraso(int tentativaQueFalhou)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tentativaQueFalhou, 1);

        // Expoente limitado para não estourar (2^30 * base já passa de qualquer teto razoável).
        var expoente = Math.Min(tentativaQueFalhou - 1, 30);
        var ticks = AtrasoBase.Ticks * (double)(1L << expoente);
        return ticks >= AtrasoMaximo.Ticks ? AtrasoMaximo : TimeSpan.FromTicks((long)ticks);
    }
}
