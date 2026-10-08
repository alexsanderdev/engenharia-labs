using System.Text.Json;
using F6M07.Cache.Catalogo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

// CA1848 (LoggerMessage source generator) é o ideal em hot path; aqui o log só ocorre em falha do Redis.
#pragma warning disable CA1848

namespace F6M07.Cache.Cache;

/// <summary>
/// Cache-aside "na mão" com StackExchange.Redis: a APLICAÇÃO lê o cache, vai à fonte no miss, grava no
/// cache com TTL e apaga a chave quando o produto muda. Redis fora do ar nunca derruba a leitura:
/// o catálogo fica mais lento, não indisponível.
/// </summary>
public sealed class CatalogoComCacheAside(
    IDatabase redis,
    IFonteDeProdutos fonte,
    PoliticaDeExpiracao politica,
    BloqueioDistribuido? bloqueio = null,
    ILogger<CatalogoComCacheAside>? logger = null)
{
    /// <summary>Valor gravado quando o produto NÃO existe ("cache negativo"), para não martelar a fonte com ids inválidos.</summary>
    public const string MarcadorAusente = "__ausente__";

    /// <summary>JSON camelCase: legível no redis-cli e estável entre versões do .NET.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly TimeSpan ValidadeDoBloqueio = TimeSpan.FromSeconds(5);
    private readonly ILogger _logger = (ILogger?)logger ?? NullLogger.Instance;

    /// <summary>Quanto tempo quem NÃO pegou o lock espera o valor aparecer antes de ir à fonte por conta própria.</summary>
    public TimeSpan EsperaMaximaPeloBloqueio { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Passo 3: cache-aside.
    /// <list type="number">
    /// <item><c>StringGetAsync(ChavesDeCache.Produto(id))</c>. Se Redis lançar (<see cref="RedisException"/> ou
    /// <see cref="RedisTimeoutException"/>), logue um warning e devolva direto da fonte (fallback).</item>
    /// <item>Hit: <see cref="MarcadorAusente"/> → null; senão desserialize o JSON.</item>
    /// <item>Miss sem <c>bloqueio</c>: <see cref="CarregarEGravarAsync"/>. Miss com <c>bloqueio</c> (Passo 5):
    /// <see cref="CarregarComBloqueioAsync"/>.</item>
    /// </list>
    /// </summary>
    public async Task<Produto?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var chave = ChavesDeCache.Produto(id);
        RedisValue emCache;
        try
        {
            emCache = await redis.StringGetAsync(chave);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Redis indisponível ao ler {Chave}; lendo da fonte", chave);
            return await fonte.ObterAsync(id, ct);
        }

        if (emCache.HasValue) return Desserializar(emCache);

        return bloqueio is null
            ? await CarregarEGravarAsync(id, chave, ct)
            : await CarregarComBloqueioAsync(id, chave, bloqueio, ct);
    }

    /// <summary>
    /// Passo 4: invalidação. Atualize a FONTE primeiro e depois APAGUE a chave (não regrave o valor:
    /// duas atualizações concorrentes poderiam deixar o cache com a versão mais velha).
    /// Falha do Redis ao apagar: logue (o TTL é a rede de segurança) — não desfaça a atualização.
    /// </summary>
    public async Task AtualizarAsync(Produto produto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(produto);
        await fonte.AtualizarAsync(produto, ct);
        var chave = ChavesDeCache.Produto(produto.Id);
        try
        {
            await redis.KeyDeleteAsync(chave);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
        {
            _logger.LogError(ex, "Não foi possível invalidar {Chave}; o valor antigo vive até o TTL", chave);
        }
    }

    /// <summary>
    /// Passo 3: lê da fonte e grava no cache: produto → JSON com <c>politica.CalcularTtl()</c>;
    /// inexistente → <see cref="MarcadorAusente"/> com <c>politica.TtlNegativo</c>. Erro do Redis ao gravar: logue e siga.
    /// </summary>
    private async Task<Produto?> CarregarEGravarAsync(Guid id, string chave, CancellationToken ct)
    {
        var produto = await fonte.ObterAsync(id, ct);
        var (valor, ttl) = produto is null
            ? ((RedisValue)MarcadorAusente, politica.TtlNegativo)
            : ((RedisValue)JsonSerializer.Serialize(produto, Json), politica.CalcularTtl());
        try
        {
            await redis.StringSetAsync(chave, valor, ttl, When.Always);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Não foi possível gravar {Chave} no cache", chave);
        }
        return produto;
    }

    /// <summary>
    /// Passo 5: proteção contra stampede.
    /// <list type="bullet">
    /// <item>Tente o lock <c>ChavesDeCache.Bloqueio(chave)</c>. Conseguiu: RELEIA o cache (outro pode ter acabado de
    /// gravar — double-check), senão <see cref="CarregarEGravarAsync"/>; libere o lock no <c>finally</c>.</item>
    /// <item>Não conseguiu: faça polling do cache (ex.: a cada 20 ms) até o valor aparecer ou estourar
    /// <see cref="EsperaMaximaPeloBloqueio"/>; estourou → vá à fonte (degrada, mas responde).</item>
    /// </list>
    /// </summary>
    private async Task<Produto?> CarregarComBloqueioAsync(Guid id, string chave, BloqueioDistribuido trava, CancellationToken ct)
    {
        var chaveDoBloqueio = ChavesDeCache.Bloqueio(chave);
        var token = await trava.TentarAdquirirAsync(chaveDoBloqueio, ValidadeDoBloqueio);
        if (token is not null)
        {
            try
            {
                var deNovo = await redis.StringGetAsync(chave);
                return deNovo.HasValue ? Desserializar(deNovo) : await CarregarEGravarAsync(id, chave, ct);
            }
            finally
            {
                await trava.LiberarAsync(chaveDoBloqueio, token);
            }
        }

        var limite = DateTime.UtcNow + EsperaMaximaPeloBloqueio;
        while (DateTime.UtcNow < limite)
        {
            await Task.Delay(20, ct);
            var valor = await redis.StringGetAsync(chave);
            if (valor.HasValue) return Desserializar(valor);
        }

        _logger.LogWarning("Esperou {Espera} pelo recarregamento de {Chave}; lendo da fonte", EsperaMaximaPeloBloqueio, chave);
        return await fonte.ObterAsync(id, ct);
    }

    private static Produto? Desserializar(RedisValue valor) =>
        valor == MarcadorAusente ? null : JsonSerializer.Deserialize<Produto>(valor.ToString(), Json);
}
