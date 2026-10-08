using F3M02.Indices.Tests.Infra;

namespace F3M02.Indices.Tests;

/// <summary>
/// Passo 3 — FK sem índice: ItensPedido tem PK (PedidoId, ProdutoId), então buscar por ProdutoId varre a tabela.
/// Um índice só em ProdutoId já troca o scan por seek, mas paga ~600 Key Lookups (~1.850 leituras): falta o INCLUDE.
/// </summary>
public sealed class Consulta02_VendasDoProdutoTests(SqlServerFixture fixture) : PlanoTestBase(fixture)
{
    private Task<AnaliseDeConsulta> AnalisarAsync() =>
        AnalisarAsync("02-VendasDoProduto.sql", Inteiro("@produtoId", 77));

    [Fact]
    public async Task VendasDoProduto_IndiceDeCoberturaNaFk_FazSeekSemLookup()
    {
        var analise = await AnalisarAsync();

        ((int)analise.Linhas.Single()[0]!).ShouldBe(600);
        analise.AcessosA("ItensPedido").ShouldNotContain(o => o.EhScan, analise.Resumo);
        analise.AcessosA("ItensPedido").ShouldContain(o => o.Operador == "Index Seek", analise.Resumo);
        analise.Operadores.ShouldNotContain(o => o.EhKeyLookup, analise.Resumo);
    }

    [Fact]
    public async Task VendasDoProduto_LeiturasLogicas_PoucasPaginas()
    {
        var analise = await AnalisarAsync();

        analise.LeiturasLogicas.ShouldBeLessThanOrEqualTo(10, analise.Resumo);
    }
}
