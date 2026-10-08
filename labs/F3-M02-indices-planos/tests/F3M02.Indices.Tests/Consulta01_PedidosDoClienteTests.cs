using F3M02.Indices.Tests.Infra;

namespace F3M02.Indices.Tests;

/// <summary>
/// Passos 1 e 2 — "Meus pedidos": de Clustered Index Scan (200 mil linhas) para Index Seek,
/// depois sem Key Lookup (INCLUDE) e sem Sort (ordem das colunas da chave).
/// </summary>
public sealed class Consulta01_PedidosDoClienteTests(SqlServerFixture fixture) : PlanoTestBase(fixture)
{
    private Task<AnaliseDeConsulta> AnalisarAsync() =>
        AnalisarAsync("01-PedidosDoCliente.sql", Inteiro("@clienteId", 4242));

    [Fact]
    public async Task PedidosDoCliente_ComIndiceEmClienteId_FazIndexSeekEmVezDeScan()
    {
        var analise = await AnalisarAsync();

        analise.Linhas.Count.ShouldBe(10);
        analise.AcessosA("Pedidos").ShouldNotContain(o => o.EhScan, analise.Resumo);
        analise.AcessosA("Pedidos").ShouldContain(o => o.Operador == "Index Seek", analise.Resumo);
    }

    [Fact]
    public async Task PedidosDoCliente_IndiceCobreAsColunas_SemKeyLookup()
    {
        var analise = await AnalisarAsync();

        analise.AcessosA("Pedidos").ShouldContain(o => o.EhSeek, analise.Resumo);
        analise.Operadores.ShouldNotContain(o => o.EhKeyLookup, analise.Resumo);
    }

    [Fact]
    public async Task PedidosDoCliente_OrdemDasColunasDaChave_DispensaOSort()
    {
        var analise = await AnalisarAsync();

        analise.AcessosA("Pedidos").ShouldContain(o => o.EhSeek, analise.Resumo);
        analise.TemOperador("Sort").ShouldBeFalse(analise.Resumo);
        analise.Linhas.Select(l => (DateTime)l[1]!).ShouldBeInOrder(SortDirection.Descending);
    }

    [Fact]
    public async Task PedidosDoCliente_LeiturasLogicas_CaemDeMilharesParaPoucasPaginas()
    {
        var analise = await AnalisarAsync();

        // Sem índice: a clusterizada inteira (milhares de páginas). Com índice de cobertura: só o caminho da árvore.
        analise.LeiturasLogicas.ShouldBeLessThanOrEqualTo(10, analise.Resumo);
    }
}
