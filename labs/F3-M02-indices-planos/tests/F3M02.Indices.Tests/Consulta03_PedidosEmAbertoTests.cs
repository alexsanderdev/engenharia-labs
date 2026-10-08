using F3M02.Indices.Tests.Infra;

namespace F3M02.Indices.Tests;

/// <summary>
/// Passo 4 — Índice filtrado: só ~1% dos pedidos está 'Created'. Indexar só essas linhas
/// dá um índice minúsculo, que cobre a fila do backoffice e quase não custa nas escritas.
/// </summary>
public sealed class Consulta03_PedidosEmAbertoTests(SqlServerFixture fixture) : PlanoTestBase(fixture)
{
    private Task<AnaliseDeConsulta> AnalisarAsync() => AnalisarAsync("03-PedidosEmAberto.sql");

    private async Task<HashSet<string>> IndicesFiltradosDePedidosAsync()
    {
        var linhas = await ConsultarAsync(
            "SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Pedidos') AND has_filter = 1");
        return linhas.Select(l => (string)l[0]!).ToHashSet();
    }

    [Fact]
    public async Task PedidosEmAberto_LePedidosPorUmIndiceFiltrado()
    {
        var analise = await AnalisarAsync();
        var filtrados = await IndicesFiltradosDePedidosAsync();

        analise.Linhas.Count.ShouldBe(50);
        filtrados.ShouldNotBeEmpty("Crie um índice com WHERE Status = 'Created'.");
        analise.AcessosA("Pedidos").ShouldNotBeEmpty(analise.Resumo);
        analise.AcessosA("Pedidos").ShouldAllBe(o => o.Indice != null && filtrados.Contains(o.Indice), analise.Resumo);
    }

    [Fact]
    public async Task PedidosEmAberto_IndiceFiltradoCobreEOrdena_SemSortESemLookup()
    {
        var analise = await AnalisarAsync();

        analise.AcessosA("Pedidos").ShouldNotContain(o => o.Operador == "Clustered Index Scan", analise.Resumo);
        analise.Operadores.ShouldNotContain(o => o.EhKeyLookup, analise.Resumo);
        analise.TemOperador("Sort").ShouldBeFalse(analise.Resumo);
        analise.LeiturasLogicas.ShouldBeLessThanOrEqualTo(5, analise.Resumo);
    }

    [Fact]
    public async Task PedidosEmAberto_IndiceFiltrado_EhPequenoPertoDaTabela()
    {
        var linhas = await ConsultarAsync("""
            SELECT i.name, i.has_filter, SUM(ps.used_page_count) AS Paginas
            FROM sys.indexes AS i
            JOIN sys.dm_db_partition_stats AS ps ON ps.object_id = i.object_id AND ps.index_id = i.index_id
            WHERE i.object_id = OBJECT_ID('dbo.Pedidos') AND (i.index_id = 1 OR i.has_filter = 1)
            GROUP BY i.name, i.has_filter
            """);

        var tabela = linhas.Where(l => !(bool)l[1]!).Sum(l => (long)l[2]!);
        var filtrados = linhas.Where(l => (bool)l[1]!).ToList();
        filtrados.ShouldNotBeEmpty("Nenhum índice filtrado em Pedidos.");
        filtrados.ShouldAllBe(l => (long)l[2]! * 100 < tabela, $"A clusterizada tem {tabela} páginas; o filtrado deveria ter menos de 1%.");
    }
}
