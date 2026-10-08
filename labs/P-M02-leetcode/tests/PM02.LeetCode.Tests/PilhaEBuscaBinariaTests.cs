namespace PM02.LeetCode.Tests;

// Semana 8
public class ValidParenthesesTests
{
    [Theory]
    [InlineData("()", true)]
    [InlineData("()[]{}", true)]
    [InlineData("{[()]}", true)]
    [InlineData("", true)]
    [InlineData("(]", false)]
    [InlineData("([)]", false)]
    [InlineData("(((", false)]   // sobrou abertura na pilha
    [InlineData("))", false)]    // fechamento com pilha vazia
    public void Resolver_ValidaAberturaEFechamentoNaOrdem(string s, bool esperado)
    {
        ValidParentheses.Resolver(s).ShouldBe(esperado);
    }
}

// Semana 9
public class DailyTemperaturesTests
{
    [Theory]
    [InlineData(new[] { 73, 74, 75, 71, 69, 72, 76, 73 }, new[] { 1, 1, 4, 2, 1, 1, 0, 0 })]
    [InlineData(new[] { 30, 40, 50, 60 }, new[] { 1, 1, 1, 0 })]
    [InlineData(new[] { 30, 60, 90 }, new[] { 1, 1, 0 })]
    [InlineData(new[] { 50, 50, 50 }, new[] { 0, 0, 0 })] // igual não é "mais quente"
    [InlineData(new[] { 90, 80, 70 }, new[] { 0, 0, 0 })]
    public void Resolver_RetornaDiasAteTemperaturaMaior(int[] temperaturas, int[] esperado)
    {
        DailyTemperatures.Resolver(temperaturas).ShouldBe(esperado);
    }
}

// Semana 10
public class BinarySearchTests
{
    [Theory]
    [InlineData(new[] { -1, 0, 3, 5, 9, 12 }, 9, 4)]
    [InlineData(new[] { -1, 0, 3, 5, 9, 12 }, 2, -1)]
    [InlineData(new[] { 5 }, 5, 0)]
    [InlineData(new[] { 5 }, -5, -1)]
    [InlineData(new int[0], 1, -1)]
    [InlineData(new[] { 1, 3 }, 3, 1)]   // armadilha de off-by-one no último elemento
    [InlineData(new[] { 1, 3 }, 1, 0)]
    public void Resolver_RetornaIndiceOuMenosUm(int[] numeros, int alvo, int esperado)
    {
        BinarySearch.Resolver(numeros, alvo).ShouldBe(esperado);
    }

    [Fact]
    public void Resolver_ValoresGrandes_NaoEstouraNoCalculoDoMeio()
    {
        var numeros = new[] { int.MaxValue - 2, int.MaxValue - 1, int.MaxValue };

        BinarySearch.Resolver(numeros, int.MaxValue).ShouldBe(2);
    }
}

// Semana 11
public class SearchInRotatedSortedArrayTests
{
    [Theory]
    [InlineData(new[] { 4, 5, 6, 7, 0, 1, 2 }, 0, 4)]
    [InlineData(new[] { 4, 5, 6, 7, 0, 1, 2 }, 3, -1)]
    [InlineData(new[] { 4, 5, 6, 7, 0, 1, 2 }, 5, 1)]
    [InlineData(new[] { 1 }, 0, -1)]
    [InlineData(new[] { 1, 3 }, 3, 1)]
    [InlineData(new[] { 3, 1 }, 1, 1)]
    [InlineData(new[] { 5, 1, 3 }, 5, 0)]
    [InlineData(new[] { 1, 2, 3, 4, 5 }, 4, 3)] // rotação zero
    public void Resolver_EncontraEmArrayRotacionado(int[] numeros, int alvo, int esperado)
    {
        SearchInRotatedSortedArray.Resolver(numeros, alvo).ShouldBe(esperado);
    }
}
