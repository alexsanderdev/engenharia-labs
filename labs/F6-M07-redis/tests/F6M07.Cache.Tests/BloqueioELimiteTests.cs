using F6M07.Cache.Cache;
using F6M07.Cache.Limites;
using F6M07.Cache.Tests.Infra;
using Microsoft.Extensions.Time.Testing;

namespace F6M07.Cache.Tests;

/// <summary>Passos 5 e 7: lock distribuído (SET NX PX + token) e rate limit com contador atômico.</summary>
[Collection(ColecaoRedis.Nome)]
public sealed class BloqueioELimiteTests(RedisFixture redis)
{
    [Fact]
    public async Task Bloqueio_SegundoInteressadoNaoAdquire_EOLockTemValidade()
    {
        var bloqueio = new BloqueioDistribuido(redis.Db);
        var chave = $"lock:teste:{Guid.NewGuid()}";

        var token1 = await bloqueio.TentarAdquirirAsync(chave, TimeSpan.FromSeconds(5));
        var token2 = await bloqueio.TentarAdquirirAsync(chave, TimeSpan.FromSeconds(5));

        token1.ShouldNotBeNullOrWhiteSpace();
        token2.ShouldBeNull();
        var ttl = await redis.Db.KeyTimeToLiveAsync(chave);
        ttl.ShouldNotBeNull("lock sem validade trava o sistema para sempre se o dono morrer");
        ttl.Value.ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Bloqueio_LiberarComTokenDeOutro_NaoApaga_ComOProprioToken_Apaga()
    {
        var bloqueio = new BloqueioDistribuido(redis.Db);
        var chave = $"lock:teste:{Guid.NewGuid()}";
        var token = await bloqueio.TentarAdquirirAsync(chave, TimeSpan.FromSeconds(5));

        (await bloqueio.LiberarAsync(chave, "token-de-outro-processo")).ShouldBeFalse();
        (await redis.Db.KeyExistsAsync(chave)).ShouldBeTrue();

        (await bloqueio.LiberarAsync(chave, token!)).ShouldBeTrue();
        (await redis.Db.KeyExistsAsync(chave)).ShouldBeFalse();
        (await bloqueio.TentarAdquirirAsync(chave, TimeSpan.FromSeconds(5))).ShouldNotBeNull("liberado, outro pode adquirir");
    }

    [Fact]
    public async Task Limitador_CincoPorJanela_SextaRequisicaoEhBloqueada()
    {
        var tempo = new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));
        var limitador = new LimitadorDistribuido(redis.Db, limite: 5, TimeSpan.FromMinutes(1), tempo);
        var cliente = $"cliente-{Guid.NewGuid():N}";

        var resultados = new List<ResultadoDoLimite>();
        for (var i = 0; i < 6; i++) resultados.Add(await limitador.TentarAsync(cliente));

        resultados.Take(5).ShouldAllBe(r => r.Permitido);
        resultados[4].Restantes.ShouldBe(0);
        resultados[5].Permitido.ShouldBeFalse();
        resultados[5].Contagem.ShouldBe(6);
        var ttl = await redis.Db.KeyTimeToLiveAsync(limitador.ChaveDaJanela(cliente));
        ttl.ShouldNotBeNull("contador sem TTL bloquearia o cliente para sempre");
        ttl.Value.ShouldBeLessThanOrEqualTo(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Limitador_ClientesSaoIndependentes_ENovaJanelaZeraAContagem()
    {
        var tempo = new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));
        var limitador = new LimitadorDistribuido(redis.Db, limite: 2, TimeSpan.FromMinutes(1), tempo);
        var parceiroA = $"parceiro-a-{Guid.NewGuid():N}";
        var parceiroB = $"parceiro-b-{Guid.NewGuid():N}";

        for (var i = 0; i < 3; i++) await limitador.TentarAsync(parceiroA);
        (await limitador.TentarAsync(parceiroA)).Permitido.ShouldBeFalse();
        (await limitador.TentarAsync(parceiroB)).Permitido.ShouldBeTrue("o laço do parceiro A não pode afetar o B");

        tempo.Advance(TimeSpan.FromMinutes(1));
        var naNovaJanela = await limitador.TentarAsync(parceiroA);
        naNovaJanela.Permitido.ShouldBeTrue();
        naNovaJanela.Contagem.ShouldBe(1);
    }

    [Fact]
    public async Task Limitador_50RequisicoesConcorrentesDeVariasInstancias_ExatamenteOLimitePassa()
    {
        var tempo = new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));
        // Três "instâncias da API" (três objetos), um Redis: o limite é global, não por instância.
        var instancias = Enumerable.Range(0, 3)
            .Select(_ => new LimitadorDistribuido(redis.Db, limite: 10, TimeSpan.FromMinutes(1), tempo)).ToArray();
        var cliente = $"cliente-{Guid.NewGuid():N}";

        var resultados = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => instancias[i % 3].TentarAsync(cliente), TestContext.Current.CancellationToken)));

        resultados.Count(r => r.Permitido).ShouldBe(10);
        resultados.Select(r => r.Contagem).Order().ShouldBe(Enumerable.Range(1, 50).Select(n => (long)n), "INCR é atômico: nenhuma contagem se repete");
    }
}
