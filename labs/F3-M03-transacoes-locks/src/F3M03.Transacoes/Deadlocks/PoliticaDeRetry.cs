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
    public TimeSpan CalcularAtraso(int tentativaQueFalhou) =>
        throw new NotImplementedException(
            "TODO (Passo 5): AtrasoBase * 2^(tentativaQueFalhou - 1), limitado a AtrasoMaximo. " +
            "Cuidado com overflow em tentativas altas (limite o expoente ou compare antes de multiplicar).");
}
