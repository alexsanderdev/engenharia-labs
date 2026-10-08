using F2M03.CodeSmells.PrimitiveObsession;

namespace F2M03.CodeSmells.Tests;

/// <summary>
/// Caracterização do cadastro legado: estes testes passam desde o início e NUNCA podem ficar vermelhos.
/// </summary>
public class ComportamentoPrimitiveObsessionTests
{
    [Fact]
    public void Cadastrar_DadosValidos_NormalizaNomeCpfEmailELimite()
    {
        var cliente = new CadastroDeClientes().Cadastrar("  Ana Souza ", "529.982.247-25", "  Ana@Exemplo.COM ", 1500.005m);

        cliente.ShouldBe(new ClienteLegado("Ana Souza", "52998224725", "ana@exemplo.com", 1500.01m));
        CadastroDeClientes.FormatarCpf("11144477735").ShouldBe("111.444.777-35");
    }

    [Theory]
    [InlineData("", "529.982.247-25", "ana@exemplo.com", 0)]
    [InlineData("Ana", "111.111.111-11", "ana@exemplo.com", 0)]
    [InlineData("Ana", "529.982.247-26", "ana@exemplo.com", 0)]
    [InlineData("Ana", "529.982.247", "ana@exemplo.com", 0)]
    [InlineData("Ana", "529.982.247-25", "ana.exemplo.com", 0)]
    [InlineData("Ana", "529.982.247-25", "ana@exemplo", 0)]
    [InlineData("Ana", "529.982.247-25", "ana@exemplo.com", -0.01)]
    public void Cadastrar_DadoInvalido_LancaArgumentException(string nome, string cpf, string email, decimal limite)
    {
        Should.Throw<ArgumentException>(() => new CadastroDeClientes().Cadastrar(nome, cpf, email, limite));
    }
}
