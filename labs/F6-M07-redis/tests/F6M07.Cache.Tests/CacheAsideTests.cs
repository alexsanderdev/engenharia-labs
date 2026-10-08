using System.Diagnostics;
using F6M07.Cache.Cache;
using F6M07.Cache.Tests.Infra;

namespace F6M07.Cache.Tests;

/// <summary>Passos 3 a 5 com Redis real: cache-aside, TTL, cache negativo, invalidação, medição e stampede.</summary>
[Collection(ColecaoRedis.Nome)]
public sealed class CacheAsideTests(RedisFixture redis)
{
    private static readonly PoliticaDeExpiracao Politica = new(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));

    [Fact]
    public async Task ObterAsync_SegundaLeitura_VemDoCacheSemIrAFonte()
    {
        var fonte = new FonteContadora();
        var produto = fonte.Adicionar();
        var catalogo = new CatalogoComCacheAside(redis.Db, fonte, Politica);
        var ct = TestContext.Current.CancellationToken;

        var primeira = await catalogo.ObterAsync(produto.Id, ct);
        var segunda = await catalogo.ObterAsync(produto.Id, ct);

        primeira.ShouldBe(produto);
        segunda.ShouldBe(produto);
        fonte.Leituras.ShouldBe(1);
    }

    [Fact]
    public async Task ObterAsync_Miss_GravaJsonLegivelComTtlDaPolitica()
    {
        var fonte = new FonteContadora();
        var produto = fonte.Adicionar(nome: "Mouse sem fio", preco: 129.90m);
        var catalogo = new CatalogoComCacheAside(redis.Db, fonte, Politica);

        await catalogo.ObterAsync(produto.Id, TestContext.Current.CancellationToken);

        var chave = ChavesDeCache.Produto(produto.Id);
        var bruto = (string?)await redis.Db.StringGetAsync(chave);
        bruto.ShouldNotBeNull();
        bruto.ShouldContain("\"nome\":\"Mouse sem fio\"");
        bruto.ShouldContain("\"preco\":129.90");
        var ttl = await redis.Db.KeyTimeToLiveAsync(chave);
        ttl.ShouldNotBeNull("chave de cache SEM TTL é vazamento de memória esperando acontecer");
        ttl.Value.ShouldBeInRange(TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(6));
    }

    [Fact]
    public async Task ObterAsync_ProdutoInexistente_GuardaCacheNegativoCurto()
    {
        var fonte = new FonteContadora();
        var catalogo = new CatalogoComCacheAside(redis.Db, fonte, Politica);
        var idInexistente = Guid.NewGuid();
        var ct = TestContext.Current.CancellationToken;

        (await catalogo.ObterAsync(idInexistente, ct)).ShouldBeNull();
        (await catalogo.ObterAsync(idInexistente, ct)).ShouldBeNull();

        fonte.Leituras.ShouldBe(1, "um id inválido repetido não pode martelar o banco");
        var chave = ChavesDeCache.Produto(idInexistente);
        ((string?)await redis.Db.StringGetAsync(chave)).ShouldBe(CatalogoComCacheAside.MarcadorAusente);
        (await redis.Db.KeyTimeToLiveAsync(chave))!.Value.ShouldBeLessThanOrEqualTo(Politica.TtlNegativo);
    }

    [Fact]
    public async Task AtualizarAsync_ApagaAChave_ProximaLeituraTrazOValorNovo()
    {
        var fonte = new FonteContadora();
        var produto = fonte.Adicionar(preco: 349.90m);
        var catalogo = new CatalogoComCacheAside(redis.Db, fonte, Politica);
        var ct = TestContext.Current.CancellationToken;
        await catalogo.ObterAsync(produto.Id, ct);

        await catalogo.AtualizarAsync(produto with { Preco = 299.90m }, ct);

        (await redis.Db.KeyExistsAsync(ChavesDeCache.Produto(produto.Id))).ShouldBeFalse("invalidar = apagar");
        (await catalogo.ObterAsync(produto.Id, ct))!.Preco.ShouldBe(299.90m);
        fonte.Leituras.ShouldBe(2);
    }

    [Fact]
    public async Task Medicao_100LeiturasDe10Produtos_ComCacheAFonteEhChamada10VezesSemCache100()
    {
        var ct = TestContext.Current.CancellationToken;
        var semCache = new FonteContadora { Latencia = TimeSpan.FromMilliseconds(2) };
        var comCacheFonte = new FonteContadora { Latencia = TimeSpan.FromMilliseconds(2) };
        var ids = Enumerable.Range(0, 10).Select(i => (semCache.Adicionar($"P{i}").Id, comCacheFonte.Adicionar($"P{i}").Id)).ToList();
        var catalogo = new CatalogoComCacheAside(redis.Db, comCacheFonte, Politica);

        var t1 = Stopwatch.StartNew();
        for (var rodada = 0; rodada < 10; rodada++)
            foreach (var (id, _) in ids) await semCache.ObterAsync(id, ct);
        t1.Stop();

        var t2 = Stopwatch.StartNew();
        for (var rodada = 0; rodada < 10; rodada++)
            foreach (var (_, id) in ids) await catalogo.ObterAsync(id, ct);
        t2.Stop();

        TestContext.Current.SendDiagnosticMessage(
            $"Sem cache: {semCache.Leituras} leituras na fonte em {t1.ElapsedMilliseconds} ms; " +
            $"com cache: {comCacheFonte.Leituras} leituras na fonte em {t2.ElapsedMilliseconds} ms.");
        semCache.Leituras.ShouldBe(100);
        comCacheFonte.Leituras.ShouldBe(10, "hit ratio de 90%: só o primeiro acesso de cada produto vai ao banco");
    }

    [Fact]
    public async Task ObterAsync_ComBloqueio_20LeiturasConcorrentesDeChaveFria_UmaSoIdaAFonte()
    {
        var fonte = new FonteContadora { Latencia = TimeSpan.FromMilliseconds(200) }; // banco lento sob pico
        var produto = fonte.Adicionar();
        var catalogo = new CatalogoComCacheAside(redis.Db, fonte, Politica, new BloqueioDistribuido(redis.Db));
        var ct = TestContext.Current.CancellationToken;

        var resultados = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => catalogo.ObterAsync(produto.Id, ct), ct)));

        resultados.ShouldAllBe(p => p == produto);
        fonte.Leituras.ShouldBe(1, "sem proteção, as 20 requisições iriam ao banco ao mesmo tempo (stampede)");
        (await redis.Db.KeyExistsAsync(ChavesDeCache.Bloqueio(ChavesDeCache.Produto(produto.Id)))).ShouldBeFalse("o lock é liberado");
    }
}
