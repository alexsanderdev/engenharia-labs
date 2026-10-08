namespace PM01.Estruturas.Tests;

public class MapaHashTests
{
    [Fact]
    public void Adicionar_TentarObter_EncontraValorPelaChave()
    {
        var mapa = new MapaHash<string, decimal>();
        mapa.Adicionar("SKU-1", 10m);
        mapa.Adicionar("SKU-2", 25.5m);

        mapa.TentarObter("SKU-2", out var preco).ShouldBeTrue();
        preco.ShouldBe(25.5m);
        mapa.TentarObter("SKU-3", out _).ShouldBeFalse();
        mapa.Count.ShouldBe(2);
    }

    [Fact]
    public void Adicionar_ChaveDuplicada_LancaArgumentException()
    {
        var mapa = new MapaHash<string, int>();
        mapa.Adicionar("a", 1);

        Should.Throw<ArgumentException>(() => mapa.Adicionar("a", 2));
    }

    [Fact]
    public void Indexador_SetEmChaveExistente_SobrescreveSemAumentarCount()
    {
        var mapa = new MapaHash<string, int>();
        mapa["estoque"] = 5;
        mapa["estoque"] = 7;

        mapa["estoque"].ShouldBe(7);
        mapa.Count.ShouldBe(1);
        Should.Throw<KeyNotFoundException>(() => mapa["inexistente"]);
    }

    [Fact]
    public void Adicionar_ChaveNula_LancaArgumentNullException()
    {
        var mapa = new MapaHash<string, int>();

        Should.Throw<ArgumentNullException>(() => mapa.Adicionar(null!, 1));
    }

    [Fact]
    public void ChavesComMesmoHash_ConvivemNoMesmoBalde()
    {
        // Comparador que manda TODAS as chaves para o mesmo hash: força colisões.
        var mapa = new MapaHash<string, int>(new HashConstante());
        mapa.Adicionar("a", 1);
        mapa.Adicionar("b", 2);
        mapa.Adicionar("c", 3);

        mapa["a"].ShouldBe(1);
        mapa["b"].ShouldBe(2);
        mapa["c"].ShouldBe(3);

        mapa.Remover("b").ShouldBeTrue();
        mapa.ContemChave("b").ShouldBeFalse();
        mapa["c"].ShouldBe(3);
        mapa.Count.ShouldBe(2);
    }

    [Fact]
    public void Adicionar_AcimaDoFatorDeCarga_RedimensionaEMantemTudo()
    {
        var mapa = new MapaHash<int, string>();
        mapa.QuantidadeDeBaldes.ShouldBe(8);

        for (var i = 0; i < 100; i++)
            mapa.Adicionar(i, $"produto-{i}");

        mapa.QuantidadeDeBaldes.ShouldBeGreaterThan(8);
        ((double)mapa.Count / mapa.QuantidadeDeBaldes).ShouldBeLessThanOrEqualTo(0.75);
        for (var i = 0; i < 100; i++)
            mapa[i].ShouldBe($"produto-{i}");
    }

    [Fact]
    public void Remover_ChaveInexistente_RetornaFalse()
    {
        var mapa = new MapaHash<string, int>();

        mapa.Remover("nada").ShouldBeFalse();
    }

    private sealed class HashConstante : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);
        public int GetHashCode(string obj) => 42;
    }
}
