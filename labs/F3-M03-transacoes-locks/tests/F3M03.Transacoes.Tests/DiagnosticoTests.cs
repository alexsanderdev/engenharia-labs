using System.Diagnostics;
using F3M03.Transacoes.Diagnostico;
using F3M03.Transacoes.Tests.Infra;
using Microsoft.Data.SqlClient;

namespace F3M03.Transacoes.Tests;

/// <summary>Passos 7 e 8: diagnosticar bloqueios com DMVs e falhar rápido com LOCK_TIMEOUT.</summary>
public sealed class DiagnosticoTests(BancoFixture fixture) : BancoTestBase(fixture)
{
    // ------------------------------------------------------------ Passo 7: Sql/QuemBloqueiaQuem.sql

    [Fact]
    public async Task ListarBloqueios_NinguemEsperando_ListaVazia()
    {
        var bloqueios = await new DiagnosticoDeBloqueio(Cs).ListarBloqueiosAsync();

        bloqueios.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListarBloqueios_LeitorEsperandoEscritor_MostraQuemBloqueiaQuemEOQue()
    {
        var escritor = await AbrirSessaoAsync("Escritor");
        var leitor = await AbrirSessaoAsync("Leitor");
        await escritor.ExecutarAsync("BEGIN TRAN; UPDATE dbo.Produtos SET Estoque = 0 WHERE Id = 2;");

        var leitura = leitor.EscalarAsync<int>("SELECT Estoque FROM dbo.Produtos WHERE Id = 2;");
        try
        {
            await EsperarBloqueioDaSessaoAsync(leitor.Spid);

            var bloqueios = await new DiagnosticoDeBloqueio(Cs).ListarBloqueiosAsync();

            var b = bloqueios.ShouldHaveSingleItem();
            b.SessaoBloqueada.ShouldBe(leitor.Spid);
            b.SessaoBloqueadora.ShouldBe(escritor.Spid);
            b.TipoDeEspera.ShouldBe("LCK_M_S");
            b.TipoDeRecurso.ShouldBe("KEY");
            b.ModoSolicitado.ShouldBe("S");
            b.Tabela.ShouldBe("Produtos");
            // dm_exec_sql_text mostra o texto como o servidor o compilou, às vezes auto-parametrizado:
            // "(@1 tinyint)SELECT [Estoque] FROM [dbo].[Produtos] WHERE [Id]=@1".
            b.ComandoBloqueado.ShouldNotBeNull().ShouldContain("Produtos");
            b.TempoDeEsperaMs.ShouldBeGreaterThanOrEqualTo(0);
        }
        finally
        {
            await escritor.ExecutarAsync("ROLLBACK;");
            await DrenarAsync(leitura);
        }
    }

    // ------------------------------------------------------------ Passo 7: Sql/LocksDaSessao.sql

    [Fact]
    public async Task ListarLocksDaSessao_UpdateDeUmaLinha_MostraAHierarquiaIXNaTabelaEPaginaEXNaChave()
    {
        var a = await AbrirSessaoAsync("A");
        await a.ExecutarAsync("BEGIN TRAN; UPDATE dbo.Produtos SET Estoque = 0 WHERE Id = 1;");

        var locks = await new DiagnosticoDeBloqueio(Cs).ListarLocksDaSessaoAsync(a.Spid);
        await a.ExecutarAsync("ROLLBACK;");

        // Intent locks "avisam" nos níveis de cima que há um lock mais fino embaixo.
        locks.ShouldContain(new LockAtivo("OBJECT", "IX", "GRANT", "Produtos"));
        locks.ShouldContain(new LockAtivo("PAGE", "IX", "GRANT", "Produtos"));
        locks.ShouldContain(new LockAtivo("KEY", "X", "GRANT", "Produtos"));
        locks.ShouldContain(l => l.TipoDeRecurso == "DATABASE" && l.Modo == "S" && l.Tabela == null);
        locks.ShouldNotContain(l => l.Tabela == "Pedidos");
    }

    [Fact]
    public async Task ListarLocksDaSessao_UpdLockHoldLockEmChaveQueNaoExiste_TravaOIntervalo()
    {
        var a = await AbrirSessaoAsync("A");
        // "Reservar o Id 99 para inserir depois" (padrão de upsert): como a linha não existe, não há
        // chave para travar; o HOLDLOCK (serializable) trava o INTERVALO onde ela entraria.
        await a.ExecutarAsync("BEGIN TRAN; SELECT Estoque FROM dbo.Produtos WITH (UPDLOCK, HOLDLOCK) WHERE Id = 99;");

        var locks = await new DiagnosticoDeBloqueio(Cs).ListarLocksDaSessaoAsync(a.Spid);
        await a.ExecutarAsync("ROLLBACK;");

        locks.ShouldContain(new LockAtivo("KEY", "RangeS-U", "GRANT", "Produtos"));
    }

    // ------------------------------------------------------------ Passo 8: LOCK_TIMEOUT

    [Fact]
    public async Task ObterEstoque_LinhaLivre_DevolveOValor()
    {
        (await new ConsultaDeEstoque(Cs).ObterEstoqueAsync(3, TimeSpan.FromMilliseconds(300))).ShouldBe(10);
    }

    [Fact]
    public async Task ObterEstoque_LinhaBloqueada_FalhaRapidoComRecursoBloqueado()
    {
        var escritor = await AbrirSessaoAsync("Escritor");
        await escritor.ExecutarAsync("BEGIN TRAN; UPDATE dbo.Produtos SET Estoque = 0 WHERE Id = 3;");

        try
        {
            var relogio = Stopwatch.StartNew();
            var ex = await Should.ThrowAsync<RecursoBloqueadoException>(
                NoMaximoAsync(new ConsultaDeEstoque(Cs).ObterEstoqueAsync(3, TimeSpan.FromMilliseconds(300)), "ObterEstoque"));
            relogio.Stop();

            ex.InnerException.ShouldBeOfType<SqlException>().Number.ShouldBe(1222);
            relogio.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5), "a espera deveria durar ~300 ms, não o CommandTimeout");
        }
        finally
        {
            await escritor.ExecutarAsync("ROLLBACK;");
        }
    }
}
