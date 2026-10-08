namespace PM02.LeetCode.Tests;

// Semana 4
public class ValidPalindromeTests
{
    [Theory]
    [InlineData("A man, a plan, a canal: Panama", true)]
    [InlineData("race a car", false)]
    [InlineData(" ", true)]
    [InlineData("", true)]
    [InlineData("0P", false)]
    [InlineData("Socorram-me, subi no ônibus em Marrocos", false)] // 'ô' != 'o' sem normalizar acentos
    [InlineData("Ame a ema", true)]
    public void Resolver_IgnoraNaoAlfanumericosEMaiusculas(string s, bool esperado)
    {
        ValidPalindrome.Resolver(s).ShouldBe(esperado);
    }
}

// Semana 5
public class ContainerWithMostWaterTests
{
    [Theory]
    [InlineData(new[] { 1, 8, 6, 2, 5, 4, 8, 3, 7 }, 49)]
    [InlineData(new[] { 1, 1 }, 1)]
    [InlineData(new[] { 4, 3, 2, 1, 4 }, 16)]
    [InlineData(new[] { 1, 2, 1 }, 2)]
    [InlineData(new[] { 0, 0 }, 0)]
    public void Resolver_RetornaMaiorArea(int[] alturas, int esperado)
    {
        ContainerWithMostWater.Resolver(alturas).ShouldBe(esperado);
    }
}

// Semana 6
public class LongestSubstringWithoutRepeatingCharactersTests
{
    [Theory]
    [InlineData("abcabcbb", 3)]
    [InlineData("bbbbb", 1)]
    [InlineData("pwwkew", 3)]
    [InlineData("", 0)]
    [InlineData(" ", 1)]
    [InlineData("dvdf", 3)]
    [InlineData("abba", 2)] // armadilha: a janela nunca pode andar para trás
    public void Resolver_RetornaTamanhoDaMaiorJanelaSemRepeticao(string s, int esperado)
    {
        LongestSubstringWithoutRepeatingCharacters.Resolver(s).ShouldBe(esperado);
    }
}

// Semana 7
public class BestTimeToBuyAndSellStockTests
{
    [Theory]
    [InlineData(new[] { 7, 1, 5, 3, 6, 4 }, 5)]
    [InlineData(new[] { 7, 6, 4, 3, 1 }, 0)]
    [InlineData(new[] { 5 }, 0)]
    [InlineData(new int[0], 0)]
    [InlineData(new[] { 2, 4, 1 }, 2)]          // o mínimo global (1) vem depois do melhor par
    [InlineData(new[] { 3, 2, 6, 5, 0, 3 }, 4)]
    public void Resolver_RetornaMaiorLucroComUmaCompraEUmaVenda(int[] precos, int esperado)
    {
        BestTimeToBuyAndSellStock.Resolver(precos).ShouldBe(esperado);
    }
}
