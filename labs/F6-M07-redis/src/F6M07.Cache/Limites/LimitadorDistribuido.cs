using StackExchange.Redis;

namespace F6M07.Cache.Limites;

/// <summary>Resultado de uma tentativa no limitador.</summary>
/// <param name="Permitido">A requisição pode seguir?</param>
/// <param name="Contagem">Quantas requisições o cliente já fez nesta janela (incluindo esta).</param>
/// <param name="Restantes">Quantas ainda cabem na janela (nunca negativo).</param>
public sealed record ResultadoDoLimite(bool Permitido, long Contagem, long Restantes);

/// <summary>
/// Rate limit de janela fixa COMPARTILHADO entre instâncias da API: o contador vive no Redis, não na memória
/// de cada pod (o limitador do ASP.NET Core visto no lab F5-M05 é por instância).
/// Mostra o Redis como "estrutura de dados compartilhada", não só como cache.
/// </summary>
public sealed class LimitadorDistribuido(IDatabase redis, int limite, TimeSpan janela, TimeProvider tempo)
{
    /// <summary>
    /// INCR + PEXPIRE numa operação atômica. Com dois comandos separados, um crash entre eles deixaria
    /// uma chave SEM TTL — e o cliente bloqueado para sempre.
    /// </summary>
    public const string ScriptIncrementar = """
        local atual = redis.call('INCR', KEYS[1])
        if atual == 1 then
            redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        return atual
        """;

    /// <summary>
    /// Passo 7: chave da janela atual: <c>ratelimit:{cliente}:{n}</c>, onde
    /// <c>n = tempo.GetUtcNow().ToUnixTimeMilliseconds() / (long)janela.TotalMilliseconds</c>.
    /// </summary>
    public string ChaveDaJanela(string cliente) =>
        throw new NotImplementedException(
            $"TODO (Passo 7): n = tempo.GetUtcNow().ToUnixTimeMilliseconds() / (long)janela.TotalMilliseconds " +
            $"(janela = {janela}, agora = {tempo.GetUtcNow():O}); devolva $\"ratelimit:{{cliente}}:{{n}}\".");

    /// <summary>
    /// Passo 7: execute <see cref="ScriptIncrementar"/> com <c>[ChaveDaJanela(cliente)]</c> e
    /// <c>[(long)janela.TotalMilliseconds]</c>; permitido se a contagem &lt;= limite.
    /// </summary>
    public Task<ResultadoDoLimite> TentarAsync(string cliente) =>
        throw new NotImplementedException(
            $"TODO (Passo 7): contagem = (long)await redis.ScriptEvaluateAsync(ScriptIncrementar, [ChaveDaJanela(cliente)], " +
            $"[(long)janela.TotalMilliseconds]) em {redis.Database}; devolva new ResultadoDoLimite(contagem <= {limite}, contagem, Math.Max(0, limite - contagem)).");
}
