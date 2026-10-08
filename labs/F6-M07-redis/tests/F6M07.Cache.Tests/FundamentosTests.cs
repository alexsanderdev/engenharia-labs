using F6M07.Cache.Cache;
using F6M07.Cache.Tests.Infra;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;

namespace F6M07.Cache.Tests;

/// <summary>Passos 1 a 4 sem Redis: chaves, TTL com jitter e fallback quando o Redis cai.</summary>
public sealed class FundamentosTests
{
    [Fact]
    public void ChaveDoProduto_TemPrefixoDeContextoVersaoEId()
    {
        var id = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");

        ChavesDeCache.Produto(id).ShouldBe("catalogo:v1:produto:6f9619ff-8b86-d011-b42d-00cf4fc964ff");
    }

    [Fact]
    public void CalcularTtl_ComJitter_FicaEntreBaseEBaseMaisJitterENaoEhSempreIgual()
    {
        var politica = new PoliticaDeExpiracao(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), new Random(42));

        var ttls = Enumerable.Range(0, 200).Select(_ => politica.CalcularTtl()).ToList();

        ttls.ShouldAllBe(t => t >= TimeSpan.FromMinutes(5) && t <= TimeSpan.FromMinutes(6));
        ttls.Distinct().Count().ShouldBeGreaterThan(100, "sem jitter, chaves aquecidas juntas expiram juntas");
        (ttls.Max() - ttls.Min()).ShouldBeGreaterThan(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task ObterAsync_RedisForaDoAr_LeDaFonteSemLancar()
    {
        var redis = Substitute.For<IDatabase>();
        redis.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Redis caiu", null, CommandStatus.Unknown));
        var fonte = new FonteContadora();
        var produto = fonte.Adicionar();
        var catalogo = new CatalogoComCacheAside(redis, fonte, new PoliticaDeExpiracao(TimeSpan.FromMinutes(5), TimeSpan.Zero));

        var lido = await catalogo.ObterAsync(produto.Id, TestContext.Current.CancellationToken);

        lido.ShouldBe(produto);
        fonte.Leituras.ShouldBe(1);
    }

    [Fact]
    public async Task AtualizarAsync_RedisForaDoAr_AtualizaAFonteMesmoAssim()
    {
        var redis = Substitute.For<IDatabase>();
        redis.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisTimeoutException(CommandFlags.None, "timeout", CommandStatus.Unknown));
        var fonte = new FonteContadora();
        var produto = fonte.Adicionar(preco: 100m);
        var catalogo = new CatalogoComCacheAside(redis, fonte, new PoliticaDeExpiracao(TimeSpan.FromMinutes(5), TimeSpan.Zero));

        await catalogo.AtualizarAsync(produto with { Preco = 90m }, TestContext.Current.CancellationToken);

        (await fonte.ObterAsync(produto.Id, TestContext.Current.CancellationToken))!.Preco.ShouldBe(90m);
        await redis.Received(1).KeyDeleteAsync(ChavesDeCache.Produto(produto.Id), Arg.Any<CommandFlags>());
    }
}
