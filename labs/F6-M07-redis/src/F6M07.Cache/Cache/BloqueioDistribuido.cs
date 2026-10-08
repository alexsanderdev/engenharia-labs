using StackExchange.Redis;

namespace F6M07.Cache.Cache;

/// <summary>
/// Lock distribuído SIMPLES com um único Redis: <c>SET chave token NX PX validade</c> para adquirir e
/// "apaga só se o valor ainda for o MEU token" (script Lua atômico) para liberar.
/// Serve para EFICIÊNCIA (evitar que 50 instâncias recarreguem a mesma chave), não para CORREÇÃO:
/// se a exclusão mútua for questão de dinheiro, use o banco (transação/constraint) — ver a Aula.
/// </summary>
public sealed class BloqueioDistribuido(IDatabase redis)
{
    /// <summary>Compara e apaga numa operação só (GET + DEL separados teriam corrida).</summary>
    public const string ScriptLiberar = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end
        """;

    /// <summary>
    /// Passo 5: tenta adquirir o lock <paramref name="chave"/> por <paramref name="validade"/>.
    /// Gere um token único (Guid), grave com <c>StringSetAsync(chave, token, validade, When.NotExists)</c>
    /// e devolva o token se gravou, ou <c>null</c> se outro processo já detém o lock.
    /// </summary>
    public Task<string?> TentarAdquirirAsync(string chave, TimeSpan validade) =>
        throw new NotImplementedException(
            "TODO (Passo 5): token = Guid.NewGuid().ToString(\"N\"); redis.StringSetAsync(chave, token, validade, When.NotExists); " +
            "devolva o token se gravou, senão null.");

    /// <summary>
    /// Passo 5: libera o lock SÓ se ele ainda for seu: execute <see cref="ScriptLiberar"/> com
    /// <c>ScriptEvaluateAsync(script, [chave], [token])</c> e devolva true se apagou (resultado 1).
    /// Isso impede apagar o lock de outro processo quando o seu já expirou.
    /// </summary>
    public Task<bool> LiberarAsync(string chave, string token) =>
        throw new NotImplementedException(
            $"TODO (Passo 5): execute redis.ScriptEvaluateAsync(ScriptLiberar, [chave], [token]) em {redis.Database} " +
            "e devolva (long)resultado == 1.");
}
