using F3M02.Indices.Tests.Infra;

namespace F3M02.Indices.Tests;

/// <summary>
/// Passo 5 — SARGabilidade: YEAR(CriadoEm)/MONTH(CriadoEm) escondem a coluna dentro de uma função,
/// e o índice em CriadoEm só pode ser VARRIDO. Reescreva como intervalo semiaberto.
/// </summary>
public sealed class Consulta04_FaturamentoDoMesTests(SqlServerFixture fixture) : PlanoTestBase(fixture)
{
    private Task<AnaliseDeConsulta> AnalisarAsync() =>
        AnalisarAsync("04-FaturamentoDoMes.sql", Inteiro("@ano", 2025), Inteiro("@mes", 3));

    [Fact]
    public async Task FaturamentoDoMes_DepoisDaReescrita_ResultadoContinuaOMesmo()
    {
        // Referência calculada por um caminho independente (varredura forçada da clusterizada).
        var esperado = (await ConsultarAsync("""
            SELECT COUNT(*), SUM(Total) FROM dbo.Pedidos WITH (INDEX(1))
            WHERE CriadoEm >= '2025-03-01' AND CriadoEm < '2025-04-01'
            """)).Single();

        var analise = await AnalisarAsync();

        var linha = analise.Linhas.Single();
        ((int)linha[0]!).ShouldBe(8928);
        ((int)linha[0]!).ShouldBe((int)esperado[0]!);
        ((decimal)linha[1]!).ShouldBe((decimal)esperado[1]!);
    }

    [Fact]
    public async Task FaturamentoDoMes_SemFuncaoNaColuna_FazIndexSeekNoIntervalo()
    {
        var analise = await AnalisarAsync();

        analise.AcessosA("Pedidos").ShouldNotContain(o => o.EhScan, analise.Resumo);
        analise.AcessosA("Pedidos").ShouldContain(o => o.Operador == "Index Seek", analise.Resumo);
        analise.Operadores.ShouldNotContain(o => o.EhKeyLookup, analise.Resumo);
    }

    [Fact]
    public async Task FaturamentoDoMes_LeiturasLogicas_SoAsPaginasDoMes()
    {
        var analise = await AnalisarAsync();

        analise.LeiturasLogicas.ShouldBeLessThanOrEqualTo(60, analise.Resumo);
    }
}
