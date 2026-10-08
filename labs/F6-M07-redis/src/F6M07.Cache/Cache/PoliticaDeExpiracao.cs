namespace F6M07.Cache.Cache;

/// <summary>
/// TTL com jitter: em vez de todas as chaves aquecidas juntas (deploy, pico) expirarem no MESMO segundo
/// e baterem juntas no banco, cada uma ganha um pouco de tempo aleatório a mais.
/// </summary>
public sealed class PoliticaDeExpiracao
{
    private readonly Random _aleatorio;

    /// <param name="ttlBase">TTL mínimo (ex.: 5 min).</param>
    /// <param name="jitterMaximo">Acréscimo aleatório máximo (ex.: 1 min).</param>
    /// <param name="aleatorio">Gerador (injete um com semente fixa nos testes).</param>
    public PoliticaDeExpiracao(TimeSpan ttlBase, TimeSpan jitterMaximo, Random? aleatorio = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttlBase, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(jitterMaximo, TimeSpan.Zero);
        TtlBase = ttlBase;
        JitterMaximo = jitterMaximo;
        _aleatorio = aleatorio ?? Random.Shared;
    }

    public TimeSpan TtlBase { get; }

    public TimeSpan JitterMaximo { get; }

    /// <summary>TTL do "cache negativo" (produto que não existe): curto, para não esconder um cadastro novo.</summary>
    public TimeSpan TtlNegativo { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Passo 2: devolve <c>TtlBase + aleatório em [0, JitterMaximo]</c> (use <c>_aleatorio.NextDouble()</c>).
    /// </summary>
    public TimeSpan CalcularTtl() =>
        throw new NotImplementedException("TODO (Passo 2): devolva TtlBase + JitterMaximo * _aleatorio.NextDouble().");
}
