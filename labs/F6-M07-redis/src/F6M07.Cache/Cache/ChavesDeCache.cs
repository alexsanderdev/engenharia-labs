namespace F6M07.Cache.Cache;

/// <summary>
/// Desenho de chaves num lugar só. Prefixo por contexto ("catalogo"), VERSÃO do formato ("v1") e
/// entidade + id. Mudou o formato do JSON? Suba para "v2": as chaves antigas expiram sozinhas pelo TTL
/// e nenhuma instância nova lê um payload que não entende (deploy gradual seguro).
/// </summary>
public static class ChavesDeCache
{
    /// <summary>Versão atual do formato dos valores do catálogo.</summary>
    public const string Versao = "v1";

    /// <summary>Tag que agrupa todas as entradas do catálogo (invalidação em massa no HybridCache).</summary>
    public const string TagCatalogo = "catalogo";

    /// <summary>
    /// Passo 1: chave do produto no formato <c>catalogo:v1:produto:{id}</c> (id no formato padrão do Guid, com hífens).
    /// </summary>
    public static string Produto(Guid id) =>
        throw new NotImplementedException("TODO (Passo 1): devolva $\"catalogo:{Versao}:produto:{id}\".");

    /// <summary>Tag de um produto específico (pronto).</summary>
    public static string TagProduto(Guid id) => $"produto:{id}";

    /// <summary>Chave do lock distribuído que protege o recarregamento de <paramref name="chave"/> (pronto).</summary>
    public static string Bloqueio(string chave) => $"lock:{chave}";
}
