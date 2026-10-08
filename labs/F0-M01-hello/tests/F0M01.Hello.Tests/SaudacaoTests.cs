namespace F0M01.Hello.Tests;

public class SaudacaoTests
{
    [Fact]
    public void Para_ComNome_RetornaSaudacaoPersonalizada()
    {
        Saudacao.Para("Ana").ShouldBe("Olá, Ana!");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Para_SemNome_SaudaOMundo(string? nome)
    {
        Saudacao.Para(nome).ShouldBe("Olá, mundo!");
    }

    [Fact]
    public void Para_RemoveEspacosNasPontas()
    {
        Saudacao.Para("  Bia ").ShouldBe("Olá, Bia!");
    }
}
