using F6M07.Cache.Cache;
using F6M07.Cache.Catalogo;
using F6M07.Cache.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;

namespace F6M07.Cache.Tests;

/// <summary>Passo 6: HybridCache (L1 memória + L2 Redis), stampede embutido e invalidação por tag.</summary>
[Collection(ColecaoRedis.Nome)]
public sealed class HybridCacheTests(RedisFixture redis)
{
    /// <summary>Uma "instância da API": seu próprio DI, L1 próprio, mesmo Redis (L2) e mesmo prefixo.</summary>
    private ServiceProvider NovaInstancia(IFonteDeProdutos fonte, string prefixo) =>
        new ServiceCollection()
            .AddSingleton(fonte)
            .AddCatalogoHibrido(redis.ConnectionString, prefixo)
            .BuildServiceProvider();

    private static string NovoPrefixo() => $"teste-{Guid.NewGuid():N}:";

    [Fact]
    public async Task ObterAsync_SegundaLeitura_NaoVaiAFonte()
    {
        var fonte = new FonteContadora();
        var produto = fonte.Adicionar();
        await using var api = NovaInstancia(fonte, NovoPrefixo());
        var catalogo = api.GetRequiredService<CatalogoHibrido>();
        var ct = TestContext.Current.CancellationToken;

        (await catalogo.ObterAsync(produto.Id, ct)).ShouldBe(produto);
        (await catalogo.ObterAsync(produto.Id, ct)).ShouldBe(produto);

        fonte.Leituras.ShouldBe(1);
    }

    [Fact]
    public async Task ObterAsync_20ChamadasConcorrentesNaMesmaInstancia_UmaSoIdaAFonte()
    {
        var fonte = new FonteContadora { Latencia = TimeSpan.FromMilliseconds(200) };
        var produto = fonte.Adicionar();
        await using var api = NovaInstancia(fonte, NovoPrefixo());
        var catalogo = api.GetRequiredService<CatalogoHibrido>();
        var ct = TestContext.Current.CancellationToken;

        var resultados = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => catalogo.ObterAsync(produto.Id, ct).AsTask()));

        resultados.ShouldAllBe(p => p == produto);
        fonte.Leituras.ShouldBe(1, "o HybridCache junta chamadas concorrentes da mesma chave");
    }

    [Fact]
    public async Task DuasInstancias_SegundaLeDoL2Redis_SemIrAFonte()
    {
        var fonte = new FonteContadora();
        var produto = fonte.Adicionar();
        var prefixo = NovoPrefixo();
        await using var instanciaA = NovaInstancia(fonte, prefixo);
        await using var instanciaB = NovaInstancia(fonte, prefixo);
        var ct = TestContext.Current.CancellationToken;

        await instanciaA.GetRequiredService<CatalogoHibrido>().ObterAsync(produto.Id, ct);
        var chaveNoRedis = prefixo + ChavesDeCache.Produto(produto.Id);
        await Apoio.Eventualmente(() => redis.Db.KeyExistsAsync(chaveNoRedis), "a instância A gravar no L2");

        var lidoPorB = await instanciaB.GetRequiredService<CatalogoHibrido>().ObterAsync(produto.Id, ct);

        lidoPorB.ShouldBe(produto);
        fonte.Leituras.ShouldBe(1, "B encontrou no Redis o que A carregou");
    }

    [Fact]
    public async Task Invalidacao_AtualizarRemoveAChave_EInvalidarPorTagLimpaOCatalogoInteiro()
    {
        var fonte = new FonteContadora();
        var teclado = fonte.Adicionar("Teclado", 300m);
        var mouse = fonte.Adicionar("Mouse", 100m);
        await using var api = NovaInstancia(fonte, NovoPrefixo());
        var catalogo = api.GetRequiredService<CatalogoHibrido>();
        var ct = TestContext.Current.CancellationToken;
        await catalogo.ObterAsync(teclado.Id, ct);
        await catalogo.ObterAsync(mouse.Id, ct);

        await catalogo.AtualizarAsync(teclado with { Preco = 280m }, ct);
        (await catalogo.ObterAsync(teclado.Id, ct))!.Preco.ShouldBe(280m);
        fonte.Leituras.ShouldBe(3);

        await catalogo.InvalidarCatalogoAsync(ct);
        await catalogo.ObterAsync(teclado.Id, ct);
        await catalogo.ObterAsync(mouse.Id, ct);
        fonte.Leituras.ShouldBe(5, "a tag 'catalogo' invalida todas as entradas de uma vez");
    }
}
