using F3M02.Indices.Tests.Infra;

namespace F3M02.Indices.Tests;

/// <summary>
/// Passo 6 — Conversão implícita: Documento é varchar(11), mas a aplicação manda nvarchar.
/// nvarchar tem precedência maior, então o SQL Server converte a COLUNA (CONVERT_IMPLICIT)
/// em toda linha, e o índice único vira scan. Corrija o tipo do lado do parâmetro.
/// </summary>
public sealed class Consulta05_ClientePorDocumentoTests(SqlServerFixture fixture) : PlanoTestBase(fixture)
{
    // Cliente 777: 10000000000 + 777 * 104729
    private const string Documento = "10081374433";

    private Task<AnaliseDeConsulta> AnalisarAsync() =>
        AnalisarAsync("05-ClientePorDocumento.sql", TextoUnicode("@documento", Documento, 11));

    [Fact]
    public async Task ClientePorDocumento_DepoisDaCorrecao_AchaOMesmoCliente()
    {
        var analise = await AnalisarAsync();

        var linha = analise.Linhas.Single();
        ((int)linha[0]!).ShouldBe(777);
        ((string)linha[1]!).ShouldBe("Cliente 777");
        ((string)linha[2]!).ShouldBe("cliente777@orderflow.dev");
    }

    [Fact]
    public async Task ClientePorDocumento_ParametroNVarchar_SemConversaoImplicitaNaColuna()
    {
        var analise = await AnalisarAsync();

        analise.AvisosDeConversao.ShouldNotContain(a => a.StartsWith("Seek Plan", StringComparison.Ordinal),
            string.Join(" | ", analise.AvisosDeConversao));
    }

    [Fact]
    public async Task ClientePorDocumento_UsaIndexSeekNoIndiceUnico()
    {
        var analise = await AnalisarAsync();

        analise.AcessosA("Clientes").ShouldNotContain(o => o.EhScan, analise.Resumo);
        analise.AcessosA("Clientes").ShouldContain(o => o.Operador == "Index Seek" && o.Indice == "UQ_Clientes_Documento", analise.Resumo);
        analise.LeiturasLogicas.ShouldBeLessThanOrEqualTo(10, analise.Resumo);
    }
}
