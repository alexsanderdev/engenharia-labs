using F6M07.Cache.Catalogo;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace F6M07.Cache.Cache;

/// <summary>
/// O mesmo catálogo com <see cref="HybridCache"/>: L1 em memória (por instância) + L2 Redis (compartilhado),
/// proteção contra stampede embutida (uma chamada à fonte por chave por instância) e invalidação por tag.
/// </summary>
public sealed class CatalogoHibrido(HybridCache cache, IFonteDeProdutos fonte)
{
    /// <summary>
    /// Passo 6: <c>cache.GetOrCreateAsync</c> com a chave <see cref="ChavesDeCache.Produto"/>, o estado
    /// <c>(fonte, id)</c> (lambda <c>static</c>, sem closure), e as tags <see cref="ChavesDeCache.TagCatalogo"/> e
    /// <see cref="ChavesDeCache.TagProduto"/>. As opções de expiração vêm do padrão registrado no DI.
    /// </summary>
    public ValueTask<Produto?> ObterAsync(Guid id, CancellationToken ct = default) =>
        cache.GetOrCreateAsync(
            ChavesDeCache.Produto(id),
            (fonte, id),
            static async (estado, token) => await estado.fonte.ObterAsync(estado.id, token),
            tags: [ChavesDeCache.TagCatalogo, ChavesDeCache.TagProduto(id)],
            cancellationToken: ct);

    /// <summary>Passo 6: atualiza a fonte e remove a entrada do produto (<c>RemoveAsync</c> com a chave).</summary>
    public async Task AtualizarAsync(Produto produto, CancellationToken ct = default)
    {
        await fonte.AtualizarAsync(produto, ct);
        await cache.RemoveAsync(ChavesDeCache.Produto(produto.Id), ct);
    }

    /// <summary>Passo 6: invalida TODO o catálogo de uma vez (ex.: reajuste de preços) com <c>RemoveByTagAsync</c>.</summary>
    public ValueTask InvalidarCatalogoAsync(CancellationToken ct = default) =>
        cache.RemoveByTagAsync(ChavesDeCache.TagCatalogo, ct);
}

/// <summary>Registro no DI do catálogo híbrido.</summary>
public static class ConfiguracaoDoCache
{
    /// <summary>
    /// Passo 6: registre
    /// <list type="bullet">
    /// <item><c>AddStackExchangeRedisCache</c> com <c>Configuration = redis</c> e <c>InstanceName = instancia</c> (vira o L2);</item>
    /// <item><c>AddHybridCache</c> com <c>MaximumPayloadBytes = 1 MB</c> e <c>DefaultEntryOptions</c>:
    /// <c>Expiration = 5 min</c> (L2) e <c>LocalCacheExpiration = 1 min</c> (L1 — curto, porque cada instância tem o seu);</item>
    /// <item><see cref="CatalogoHibrido"/> como singleton.</item>
    /// </list>
    /// A <see cref="IFonteDeProdutos"/> é registrada por quem chama.
    /// </summary>
    public static IServiceCollection AddCatalogoHibrido(this IServiceCollection services, string redis, string instancia)
    {
        services.AddStackExchangeRedisCache(o =>
        {
            o.Configuration = redis;
            o.InstanceName = instancia;
        });
        services.AddHybridCache(o =>
        {
            o.MaximumPayloadBytes = 1024 * 1024;
            o.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1),
            };
        });
        services.AddSingleton<CatalogoHibrido>();
        return services;
    }
}
