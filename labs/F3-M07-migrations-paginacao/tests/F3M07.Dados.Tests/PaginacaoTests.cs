using F3M07.Dados.Modelo;
using F3M07.Dados.Paginacao;
using F3M07.Dados.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace F3M07.Dados.Tests;

/// <summary>Passos 8 e 9: offset × keyset.</summary>
[Collection(ColecaoBancoPrincipal.Nome)]
public sealed class PaginacaoTests(SqlServerFixture sql) : BancoPrincipalTestBase(sql)
{
    private static readonly DateTime Base = new(2026, 3, 1, 8, 0, 0);

    /// <summary>
    /// 30 pedidos em grupos de 3 com o MESMO CriadoEm (empates!), um grupo por minuto.
    /// Com páginas de 10, a fronteira entre páginas cai no meio de um grupo de empate.
    /// </summary>
    private async Task<List<Pedido>> SemearTrintaComEmpatesAsync()
    {
        var clienteId = await SemearClienteAsync();
        await using var db = NovoContexto();
        var pedidos = Enumerable.Range(0, 30)
            .Select(i => new Pedido { ClienteId = clienteId, CriadoEm = Base.AddMinutes(i / 3), Total = 10m + i })
            .ToList();
        db.Pedidos.AddRange(pedidos);
        await db.SaveChangesAsync(Ct);
        return pedidos;
    }

    private static List<int> OrdemEsperada(IEnumerable<Pedido> pedidos) =>
        [.. pedidos.OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id).Select(p => p.Id)];

    private async Task InserirMaisNovosAsync(int quantidade)
    {
        var clienteId = await SemearClienteAsync("Novo");
        await using var db = NovoContexto();
        db.Pedidos.AddRange(Enumerable.Range(0, quantidade)
            .Select(i => new Pedido { ClienteId = clienteId, CriadoEm = Base.AddDays(1).AddMinutes(i), Total = 1m }));
        await db.SaveChangesAsync(Ct);
    }

    // ---------- Passo 8: offset ----------

    [Fact]
    public async Task Offset_PaginasEmSequencia_SeguemAOrdemCriadoEmDescIdDesc()
    {
        var pedidos = await SemearTrintaComEmpatesAsync();
        await using var db = NovoContexto();
        var paginacao = new PaginacaoDePedidos(db);

        var pagina1 = await paginacao.ListarPorOffsetAsync(1, 10, Ct);
        var pagina3 = await paginacao.ListarPorOffsetAsync(3, 10, Ct);

        var esperada = OrdemEsperada(pedidos);
        pagina1.Select(p => p.Id).ShouldBe(esperada.Take(10));
        pagina3.Select(p => p.Id).ShouldBe(esperada.Skip(20));
    }

    // ---------- Passo 9: keyset ----------

    [Fact]
    public async Task Keyset_PercorrendoTodasAsPaginas_NaoPulaNemRepeteMesmoComEmpates()
    {
        var pedidos = await SemearTrintaComEmpatesAsync();
        await using var db = NovoContexto();
        var paginacao = new PaginacaoDePedidos(db);

        var vistos = new List<int>();
        string? cursor = null;
        var paginas = 0;
        do
        {
            var pagina = await paginacao.ListarPorKeysetAsync(cursor, 7, Ct);
            vistos.AddRange(pagina.Itens.Select(p => p.Id));
            cursor = pagina.ProximoCursor;
            paginas++;
        } while (cursor is not null && paginas < 100);

        vistos.ShouldBe(OrdemEsperada(pedidos));
        paginas.ShouldBe(5); // 7 + 7 + 7 + 7 + 2; a última página devolve ProximoCursor = null
    }

    [Fact]
    public async Task NovosPedidosEntrePaginas_OffsetRepeteItens_KeysetContinuaDeOndeParou()
    {
        var pedidos = await SemearTrintaComEmpatesAsync();
        var esperada = OrdemEsperada(pedidos);
        await using var db = NovoContexto();
        var paginacao = new PaginacaoDePedidos(db);

        var offset1 = await paginacao.ListarPorOffsetAsync(1, 10, Ct);
        var keyset1 = await paginacao.ListarPorKeysetAsync(null, 10, Ct);
        keyset1.Itens.Select(p => p.Id).ShouldBe(offset1.Select(p => p.Id));

        // Enquanto o usuário lê a página 1, entram 4 pedidos novos (que vão para o TOPO da lista).
        await InserirMaisNovosAsync(4);

        var offset2 = await paginacao.ListarPorOffsetAsync(2, 10, Ct);
        var keyset2 = await paginacao.ListarPorKeysetAsync(keyset1.ProximoCursor, 10, Ct);

        // OFFSET 10 agora "anda para trás" 4 posições: os 4 últimos da página 1 aparecem de novo.
        offset2.Select(p => p.Id).Intersect(offset1.Select(p => p.Id)).Count().ShouldBe(4);
        // Keyset ancora no último item visto: exatamente os itens 11 a 20 originais.
        keyset2.Itens.Select(p => p.Id).ShouldBe(esperada.Skip(10).Take(10));
    }

    [Fact]
    public async Task Keyset_CursorAdulterado_LancaCursorInvalido()
    {
        await using var db = NovoContexto();

        await Should.ThrowAsync<CursorInvalidoException>(
            () => new PaginacaoDePedidos(db).ListarPorKeysetAsync("nao-sou-um-cursor", 10, Ct));
    }

    [Fact]
    public async Task PaginaProfunda_KeysetLeMuitoMenosPaginasDoQueOffset()
    {
        // 20 mil pedidos gerados no próprio SQL Server (rápido), um por segundo.
        var clienteId = await SemearClienteAsync();
        await Diagnostico.ExecutarAsync(ConnectionString, $"""
            INSERT INTO Pedidos (ClienteId, CriadoEm, Status, Total)
            SELECT TOP (20000) {clienteId}, DATEADD(SECOND, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), '2026-01-01'), N'Completed', 50.00
            FROM sys.all_objects AS a CROSS JOIN sys.all_objects AS b;
            UPDATE STATISTICS Pedidos WITH FULLSCAN;
            """, Ct);

        await using var db = NovoContexto();
        var paginacao = new PaginacaoDePedidos(db);

        // Página 1.901 (posições 19.001 a 19.010). O keyset parte do último item da página 1.900.
        var anterior = await paginacao.ListarPorOffsetAsync(1900, 10, Ct);
        var ultimoVisto = new PosicaoCursor(anterior[^1].CriadoEm, anterior[^1].Id);
        var consultaOffset = paginacao.ConsultaPorOffset(1901, 10);
        var consultaKeyset = paginacao.ConsultaPorKeyset(ultimoVisto, 10);

        // Mesmo resultado...
        (await consultaKeyset.Select(p => p.Id).ToListAsync(Ct)).ShouldBe(await consultaOffset.Select(p => p.Id).ToListAsync(Ct));

        // ...custo muito diferente: OFFSET lê e descarta 19.000 linhas; keyset faz um seek e lê 10.
        var leiturasOffset = await Diagnostico.LeiturasLogicasAsync(ConnectionString, consultaOffset.ToQueryString(), Ct);
        var leiturasKeyset = await Diagnostico.LeiturasLogicasAsync(ConnectionString, consultaKeyset.ToQueryString(), Ct);

        TestContext.Current.TestOutputHelper?.WriteLine($"Leituras lógicas — offset: {leiturasOffset}, keyset: {leiturasKeyset}");
        leiturasKeyset.ShouldBeLessThan(10, "Keyset deveria ser um Index Seek em IX_Pedidos_CriadoEm_Id (cobrindo as colunas).");
        leiturasOffset.ShouldBeGreaterThan(leiturasKeyset * 10);
    }
}
