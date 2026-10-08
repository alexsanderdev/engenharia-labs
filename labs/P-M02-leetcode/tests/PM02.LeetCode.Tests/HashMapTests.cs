namespace PM02.LeetCode.Tests;

// Semana 1
public class TwoSumTests
{
    [Theory]
    [InlineData(new[] { 2, 7, 11, 15 }, 9, 0, 1)]
    [InlineData(new[] { 3, 2, 4 }, 6, 1, 2)]
    [InlineData(new[] { 3, 3 }, 6, 0, 1)]
    [InlineData(new[] { -3, 4, 3, 90 }, 0, 0, 2)]
    public void Resolver_ExistePar_RetornaIndices(int[] numeros, int alvo, int i, int j)
    {
        TwoSum.Resolver(numeros, alvo).ShouldBe([i, j]);
    }

    [Fact]
    public void Resolver_NaoExistePar_RetornaVazio()
    {
        TwoSum.Resolver([1, 2, 3], 100).ShouldBeEmpty();
    }

    [Fact]
    public void Resolver_NaoUsaOMesmoElementoDuasVezes()
    {
        // 3 + 3 = 6, mas só existe UM 3: não pode responder [0, 0].
        TwoSum.Resolver([3, 1, 5], 6).ShouldBe([1, 2]);
    }
}

// Semana 2
public class ValidAnagramTests
{
    [Theory]
    [InlineData("anagram", "nagaram", true)]
    [InlineData("rat", "car", false)]
    [InlineData("", "", true)]
    [InlineData("a", "ab", false)]
    [InlineData("aab", "abb", false)]
    [InlineData("pão", "ãop", true)]
    public void Resolver_ComparaFrequencias(string s, string t, bool esperado)
    {
        ValidAnagram.Resolver(s, t).ShouldBe(esperado);
    }
}

// Semana 3
public class GroupAnagramsTests
{
    [Fact]
    public void Resolver_AgrupaPalavrasComMesmasLetras()
    {
        var grupos = GroupAnagrams.Resolver(["eat", "tea", "tan", "ate", "nat", "bat"]);

        Normalizar(grupos).ShouldBe(["ate,eat,tea", "bat", "nat,tan"]);
    }

    [Fact]
    public void Resolver_StringVaziaEUnica_FormaGrupoProprio()
    {
        Normalizar(GroupAnagrams.Resolver([""])).ShouldBe([""]);
        Normalizar(GroupAnagrams.Resolver(["", "b", ""])).ShouldBe([",", "b"]);
    }

    // A ordem dos grupos e dentro dos grupos não importa: ordenamos para comparar.
    private static string[] Normalizar(IEnumerable<IEnumerable<string>> grupos) =>
        grupos.Select(g => string.Join(',', g.Order(StringComparer.Ordinal)))
              .Order(StringComparer.Ordinal)
              .ToArray();
}

// Semana 12
public class TopKFrequentElementsTests
{
    [Theory]
    [InlineData(new[] { 1, 1, 1, 2, 2, 3 }, 2, new[] { 1, 2 })]
    [InlineData(new[] { 1 }, 1, new[] { 1 })]
    [InlineData(new[] { 4, 4, 5, 5, 5, 6, -1, -1, -1, -1 }, 2, new[] { -1, 5 })]
    [InlineData(new[] { 7, 8, 9 }, 3, new[] { 7, 8, 9 })]
    public void Resolver_RetornaOsKMaisFrequentes(int[] numeros, int k, int[] esperado)
    {
        TopKFrequentElements.Resolver(numeros, k).Order().ShouldBe(esperado.Order());
    }
}
