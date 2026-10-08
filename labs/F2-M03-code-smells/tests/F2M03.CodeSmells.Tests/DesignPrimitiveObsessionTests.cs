using F2M03.CodeSmells.PrimitiveObsession;

namespace F2M03.CodeSmells.Tests;

/// <summary>Design: value objects Cpf, Email e Dinheiro. Começam vermelhos.</summary>
public class DesignPrimitiveObsessionTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725", "529.982.247-25")]
    [InlineData("11144477735", "11144477735", "111.444.777-35")]
    public void Cpf_Criar_ValidoComOuSemMascara_GuardaDigitosFormataEComparaPorValor(string entrada, string numero, string formatado)
    {
        var cpf = Cpf.Criar(entrada);

        cpf.Numero.ShouldBe(numero);
        cpf.Formatado.ShouldBe(formatado);
        cpf.ShouldBe(Cpf.Criar(formatado)); // value object: igualdade por valor
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("111.111.111-11")]
    [InlineData("529.982.247-26")]
    public void Cpf_Criar_Invalido_LancaArgumentException(string? entrada)
    {
        Should.Throw<ArgumentException>(() => Cpf.Criar(entrada));
    }

    [Theory]
    [InlineData("ana@exemplo.com", "ana@exemplo.com")]
    [InlineData("  Ana.Souza@Exemplo.COM.br ", "ana.souza@exemplo.com.br")]
    public void Email_Criar_Valido_Normaliza(string entrada, string esperado)
    {
        Email.Criar(entrada).Endereco.ShouldBe(esperado);
        Email.Criar(entrada).ShouldBe(Email.Criar(esperado));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ana.exemplo.com")]
    [InlineData("@exemplo.com")]
    [InlineData("ana@exemplo")]
    [InlineData("ana@@exemplo.com")]
    [InlineData("ana@.com")]
    [InlineData("ana souza@exemplo.com")]
    public void Email_Criar_Invalido_LancaArgumentException(string? entrada)
    {
        Should.Throw<ArgumentException>(() => Email.Criar(entrada));
    }

    [Fact]
    public void Dinheiro_ArredondaSomaMultiplicaENaoAceitaNegativo()
    {
        new Dinheiro(10.005m).Valor.ShouldBe(10.01m);
        (new Dinheiro(10.10m) + new Dinheiro(0.20m)).ShouldBe(new Dinheiro(10.30m));
        (new Dinheiro(19.90m) * 3).ShouldBe(new Dinheiro(59.70m));
        Dinheiro.Zero.Valor.ShouldBe(0m);

        Should.Throw<ArgumentException>(() => new Dinheiro(-0.01m));
    }
}
