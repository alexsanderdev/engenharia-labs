using F3M01.Sql.Tests.Infra;
using Microsoft.Data.SqlClient;

namespace F3M01.Sql.Tests;

/// <summary>
/// Parte B: cada teste executa um arquivo de <c>src/F3M01.Sql/Sql/ParteB/</c> no banco F3M01Consultas
/// (massa em <c>Infra/BaseConsultas.sql</c>) e compara o resultado, linha a linha e NA ORDEM pedida.
/// As colunas são lidas pelo nome: respeite os aliases do cabeçalho de cada arquivo.
/// </summary>
public sealed class ParteB_ConsultasTests(SqlServerFixture fixture)
{
    private async Task<List<Linha>> ExecutarAsync(string arquivo)
    {
        var sql = ScriptsSql.Ler($"ParteB/{arquivo}");
        await using var conexao = new SqlConnection(fixture.ConsultasConnectionString);
        await conexao.OpenAsync(TestContext.Current.CancellationToken);
        await using var cmd = new SqlCommand(sql, conexao);
        return await Linha.LerTodasAsync(cmd);
    }

    [Fact]
    public async Task Consulta01_PedidosDeMarco_JuntaClienteEIncluiOUltimoDiaDoMes()
    {
        var linhas = await ExecutarAsync("01-PedidosDeMarco.sql");

        linhas.Select(l => (l.Inteiro("PedidoId"), l.Texto("Cliente"), l.Texto("Status"), l.Valor("Total"))).ShouldBe(
        [
            (4, "Ana Souza", "Completed", 120.00m),
            (14, "Elisa Martins", "Completed", 940.00m),
            (9, "Carla Dias", "Created", 800.00m),
            (12, "Diego Rocha", "Completed", 420.00m),
            (13, "Diego Rocha", "Completed", 350.00m),
            (16, "Elisa Martins", "Created", 40.00m), // 31/03 às 21h: BETWEEN '...03-31' perde este
        ]);
    }

    [Fact]
    public async Task Consulta02_FaturamentoPorProduto_UsaPrecoDoItemEIgnoraCancelados()
    {
        var linhas = await ExecutarAsync("02-FaturamentoPorProduto.sql");

        linhas.Select(l => (l.Texto("Sku"), l.Inteiro("Unidades"), l.Valor("Receita"))).ShouldBe(
        [
            ("MON-001", 3, 5400.00m),
            ("HEA-001", 4, 1800.00m),
            ("TEC-001", 3, 1020.00m), // 320 (promoção no pedido 1) + 350 + 350
            ("MOU-001", 7, 840.00m),
            ("WEB-001", 2, 600.00m),
            ("CAB-001", 12, 480.00m),
        ]);
        linhas[0].Texto("Produto").ShouldBe("Monitor 27\"");
    }

    [Fact]
    public async Task Consulta03_ClientesFieis_FiltraLinhasNoWhereEGruposNoHaving()
    {
        var linhas = await ExecutarAsync("03-ClientesFieis.sql");

        linhas.Select(l => (l.Inteiro("ClienteId"), l.Texto("Nome"), l.Inteiro("Pedidos"), l.Valor("TotalGasto"))).ShouldBe(
        [
            (1, "Ana Souza", 3, 2480.00m),
            (4, "Diego Rocha", 3, 970.00m),
            (2, "Bruno Lima", 3, 950.00m),
        ]);
    }

    [Fact]
    public async Task Consulta04_ClientesSemPedidos_LeftJoinDevolveSoQuemNuncaComprou()
    {
        var linhas = await ExecutarAsync("04-ClientesSemPedidos.sql");

        linhas.Select(l => (l.Inteiro("ClienteId"), l.Texto("Nome"))).ShouldBe(
        [
            (7, "Gabriela Alves"),
            (8, "Heitor Costa"),
        ]);
    }

    [Fact]
    public async Task Consulta05_ProdutosJaVendidos_SemiJoinSemDuplicatas()
    {
        var linhas = await ExecutarAsync("05-ProdutosJaVendidos.sql");

        linhas.Select(l => (l.Inteiro("ProdutoId"), l.Texto("Sku"))).ShouldBe(
        [
            (1, "TEC-001"), (2, "MOU-001"), (3, "MON-001"), (4, "CAB-001"), (5, "HEA-001"), (6, "WEB-001"),
        ]);
    }

    [Fact]
    public async Task Consulta06_ClientesQueNaoIndicaram_NaoCaiNaArmadilhaDoNotInComNull()
    {
        var linhas = await ExecutarAsync("06-ClientesQueNaoIndicaram.sql");

        // Com "NOT IN (SELECT IndicadoPorId ...)" o resultado vem VAZIO: a subconsulta tem NULL.
        linhas.Select(l => (l.Inteiro("ClienteId"), l.Texto("Nome"))).ShouldBe(
        [
            (4, "Diego Rocha"),
            (6, "Fábio Nunes"),
            (7, "Gabriela Alves"),
            (8, "Heitor Costa"),
        ]);
    }

    [Fact]
    public async Task Consulta07_FaturamentoMensal_CteComTicketMedioArredondado()
    {
        ScriptsSql.Ler("ParteB/07-FaturamentoMensal.sql")
            .ShouldMatch(@"(?is)\bWITH\s+\w+\s*(\([^)]*\))?\s*AS\s*\(", "Resolva com uma CTE (WITH Nome AS (...)).");

        var linhas = await ExecutarAsync("07-FaturamentoMensal.sql");

        linhas.Select(l => (l.Texto("Mes"), l.Inteiro("Pedidos"), l.Valor("Faturamento"), Math.Round(l.Valor("TicketMedio"), 2))).ShouldBe(
        [
            ("2026-01", 5, 6520.00m, 1304.00m),
            ("2026-02", 3, 950.00m, 316.67m),
            ("2026-03", 6, 2670.00m, 445.00m),
        ]);
    }

    [Fact]
    public async Task Consulta08_MaiorPedidoPorCliente_RowNumberPorParticao()
    {
        var linhas = await ExecutarAsync("08-MaiorPedidoPorCliente.sql");

        linhas.Select(l => (l.Inteiro("ClienteId"), l.Inteiro("PedidoId"), l.Valor("Total"))).ShouldBe(
        [
            (1, 2, 1800.00m),
            (2, 7, 450.00m),
            (3, 8, 3600.00m),
            (4, 12, 420.00m),
            (5, 14, 940.00m),
            (6, 15, 360.00m),
        ]);
    }

    [Fact]
    public async Task Consulta09_TotalAcumulado_SomaLinhaALinhaMesmoComDataEmpatada()
    {
        var linhas = await ExecutarAsync("09-TotalAcumulado.sql");

        // Pedidos 6 e 7 têm o MESMO CriadoEm: com o frame padrão (RANGE) os dois mostrariam 950.
        linhas.Select(l => (l.Inteiro("ClienteId"), l.Inteiro("PedidoId"), l.Valor("Total"), l.Valor("Acumulado"))).ShouldBe(
        [
            (1, 1, 560.00m, 560.00m),
            (1, 2, 1800.00m, 2360.00m),
            (1, 4, 120.00m, 2480.00m),
            (2, 5, 200.00m, 200.00m),
            (2, 6, 300.00m, 500.00m),
            (2, 7, 450.00m, 950.00m),
            (3, 8, 3600.00m, 3600.00m),
            (3, 9, 800.00m, 4400.00m),
            (4, 10, 200.00m, 200.00m),
            (4, 12, 420.00m, 620.00m),
            (4, 13, 350.00m, 970.00m),
            (5, 14, 940.00m, 940.00m),
            (5, 16, 40.00m, 980.00m),
            (6, 15, 360.00m, 360.00m),
        ]);
    }

    [Fact]
    public async Task Consulta10_RankingDeProdutos_EmpateDividePosicaoENaoCortaLinhas()
    {
        var linhas = await ExecutarAsync("10-RankingDeProdutos.sql");

        // TOP (4) ou ROW_NUMBER cortariam um dos empatados na 4ª posição.
        linhas.Select(l => (l.Inteiro("Posicao"), l.Texto("Sku"), l.Inteiro("Unidades"))).ShouldBe(
        [
            (1, "CAB-001", 12),
            (2, "MOU-001", 7),
            (3, "HEA-001", 4),
            (4, "MON-001", 3),
            (4, "TEC-001", 3),
        ]);
    }
}
