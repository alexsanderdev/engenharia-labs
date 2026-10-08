using System.Text.RegularExpressions;
using F3M07.Dados.Persistencia;
using F3M07.Dados.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace F3M07.Dados.Tests;

/// <summary>
/// Passos 1 a 4: as SUAS migrations, aplicadas em bancos vazios e descartáveis (um por teste).
/// Nomes esperados: "Inicial" e "AdicionaCanalAoPedido".
/// </summary>
public sealed partial class MigrationsTests(SqlServerFixture sql)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private LojaDbContext NovoContexto(string banco) => new(SqlServerFixture.Options(sql.ConnectionStringPara(banco)));

    private static string[] Migrations(LojaDbContext db) => [.. db.Database.GetMigrations()];

    // ---------- Passo 2: migration inicial ----------

    [Fact]
    public async Task Migrations_ExistemDuasNaOrdem_InicialEDepoisAdicionaCanal()
    {
        await using var db = NovoContexto("nao_conecta");

        var migrations = Migrations(db);

        migrations.Length.ShouldBe(2, "Crie exatamente duas migrations: Inicial e AdicionaCanalAoPedido.");
        migrations[0].ShouldEndWith("_Inicial");
        migrations[1].ShouldEndWith("_AdicionaCanalAoPedido");
    }

    [Fact]
    public async Task Modelo_ComparadoAoSnapshot_NaoTemMudancaSemMigration()
    {
        await using var db = NovoContexto("nao_conecta");

        // Mudou o modelo e esqueceu o "dotnet ef migrations add"? Este teste pega (o MigrateAsync também recusaria).
        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task MigrateAsync_BancoVazio_CriaTabelasEHistoricoSemPendencias()
    {
        var banco = SqlServerFixture.NovoBanco("mig_vazio");
        await using var db = NovoContexto(banco);

        await db.Database.MigrateAsync(Ct);

        (await db.Database.GetAppliedMigrationsAsync(Ct)).Count().ShouldBe(2);
        (await db.Database.GetPendingMigrationsAsync(Ct)).ShouldBeEmpty();
        var tabelas = await db.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
            .ToListAsync(Ct);
        tabelas.ShouldBe(["__EFMigrationsHistory", "Clientes", "Pedidos"], ignoreOrder: true);
    }

    [Fact]
    public async Task MigrationInicial_SemCanal_ComVersaoRowversionEIndiceDaPaginacao()
    {
        var banco = SqlServerFixture.NovoBanco("mig_inicial");
        await using var db = NovoContexto(banco);

        await db.GetService<IMigrator>().MigrateAsync(Migrations(db)[0], Ct);

        var colunas = await Diagnostico.ColunasAsync(sql.ConnectionStringPara(banco), "Pedidos", Ct);
        colunas.Select(c => c.Nome).ShouldNotContain("Canal", "Canal só pode nascer na SEGUNDA migration.");
        colunas.Single(c => c.Nome == "Versao").Tipo.ShouldBe("timestamp"); // rowversion no catálogo

        var indice = await Diagnostico.EscalarAsync<int>(sql.ConnectionStringPara(banco),
            "SELECT COUNT(*) FROM sys.indexes WHERE name = 'IX_Pedidos_CriadoEm_Id' AND object_id = OBJECT_ID('dbo.Pedidos')", Ct);
        indice.ShouldBe(1);
    }

    // ---------- Passo 3: coluna NOT NULL numa tabela com dados ----------

    [Fact]
    public async Task AdicionaCanal_TabelaComDados_PreservaLinhasEFazBackfillComLegado()
    {
        var banco = SqlServerFixture.NovoBanco("mig_backfill");
        var cs = sql.ConnectionStringPara(banco);
        await using var db = NovoContexto(banco);
        var migrador = db.GetService<IMigrator>();

        // Produção "antes do deploy": só a migration inicial e pedidos já gravados.
        await migrador.MigrateAsync(Migrations(db)[0], Ct);
        await Diagnostico.ExecutarAsync(cs, """
            INSERT INTO Clientes (Nome) VALUES (N'Ana');
            INSERT INTO Pedidos (ClienteId, CriadoEm, Status, Total)
            VALUES (1, '2026-01-10', N'Completed', 100.00),
                   (1, '2026-01-11', N'Created',   250.50),
                   (1, '2026-01-12', N'Cancelled',  80.00);
            """, Ct);

        // Deploy: aplica o resto.
        await migrador.MigrateAsync(null, Ct);

        var canal = (await Diagnostico.ColunasAsync(cs, "Pedidos", Ct)).Single(c => c.Nome == "Canal");
        canal.AceitaNull.ShouldBeFalse("Canal tem que terminar NOT NULL.");
        (await Diagnostico.EscalarAsync<int>(cs, "SELECT COUNT(*) FROM Pedidos", Ct)).ShouldBe(3);
        (await Diagnostico.EscalarAsync<decimal>(cs, "SELECT SUM(Total) FROM Pedidos", Ct)).ShouldBe(430.50m);
        (await Diagnostico.EscalarAsync<int>(cs, "SELECT COUNT(*) FROM Pedidos WHERE Canal = N'Legado'", Ct))
            .ShouldBe(3, "Pedidos que existiam antes da coluna devem ficar com Canal = 'Legado' (backfill), não 'Web'.");
    }

    [Fact]
    public async Task AdicionaCanal_InsertDaVersaoAntigaDoApp_RecebeDefaultWeb()
    {
        var banco = SqlServerFixture.NovoBanco("mig_default");
        var cs = sql.ConnectionStringPara(banco);
        await using var db = NovoContexto(banco);
        await db.Database.MigrateAsync(Ct);

        // A versão anterior da aplicação não conhece "Canal" e continua rodando durante o deploy.
        await Diagnostico.ExecutarAsync(cs, """
            INSERT INTO Clientes (Nome) VALUES (N'Bruno');
            INSERT INTO Pedidos (ClienteId, CriadoEm, Status, Total) VALUES (1, '2026-03-01', N'Created', 10.00);
            """, Ct);

        (await Diagnostico.EscalarAsync<string>(cs, "SELECT Canal FROM Pedidos", Ct)).ShouldBe("Web");
    }

    [Fact]
    public async Task AdicionaCanal_Down_VoltaParaInicialSemPerderPedidos()
    {
        var banco = SqlServerFixture.NovoBanco("mig_down");
        var cs = sql.ConnectionStringPara(banco);
        await using var db = NovoContexto(banco);
        var migrador = db.GetService<IMigrator>();
        await migrador.MigrateAsync(null, Ct);
        await Diagnostico.ExecutarAsync(cs, """
            INSERT INTO Clientes (Nome) VALUES (N'Carla');
            INSERT INTO Pedidos (ClienteId, CriadoEm, Status, Total) VALUES (1, '2026-03-01', N'Created', 10.00);
            """, Ct);

        await migrador.MigrateAsync(Migrations(db)[0], Ct);

        (await Diagnostico.ColunasAsync(cs, "Pedidos", Ct)).Select(c => c.Nome).ShouldNotContain("Canal");
        (await Diagnostico.EscalarAsync<int>(cs, "SELECT COUNT(*) FROM Pedidos", Ct)).ShouldBe(1);
    }

    // ---------- Passo 4: script idempotente ----------

    private static string CaminhoDoScript => Path.Combine(AppContext.BaseDirectory, "Scripts", "migrations-idempotente.sql");

    private static string LerScript()
    {
        File.Exists(CaminhoDoScript).ShouldBeTrue(
            "Gere src/F3M07.Dados/Scripts/migrations-idempotente.sql com: dotnet ef migrations script --idempotent " +
            "--project labs/F3-M07-migrations-paginacao/src/F3M07.Dados -o labs/F3-M07-migrations-paginacao/src/F3M07.Dados/Scripts/migrations-idempotente.sql");
        return File.ReadAllText(CaminhoDoScript);
    }

    [Fact]
    public async Task ScriptIdempotente_Gerado_ContemTodasAsMigrationsEChecaOHistorico()
    {
        var script = LerScript();
        await using var db = NovoContexto("nao_conecta");

        Migrations(db).ShouldNotBeEmpty("Crie as migrations antes de gerar o script.");
        foreach (var migration in Migrations(db))
            script.ShouldContain($"WHERE [MigrationId] = N'{migration}'", customMessage: $"Script desatualizado: gere de novo depois de criar {migration}.");
    }

    [Fact]
    public async Task ScriptIdempotente_ExecutadoDuasVezesNoMesmoBanco_NaoFalhaECriaOSchema()
    {
        var script = LerScript();
        var banco = SqlServerFixture.NovoBanco("mig_script");
        var cs = sql.ConnectionStringPara(banco);
        await sql.CriarBancoVazioAsync(banco, Ct);

        // "GO" não é T-SQL: é separador de lotes do sqlcmd/SSMS. Executamos lote a lote.
        var lotes = SeparadorGo().Split(script).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        for (var execucao = 1; execucao <= 2; execucao++)
        {
            foreach (var lote in lotes)
                await Diagnostico.ExecutarAsync(cs, lote, Ct);
        }

        (await Diagnostico.EscalarAsync<int>(cs, "SELECT COUNT(*) FROM __EFMigrationsHistory", Ct)).ShouldBe(2);
        (await Diagnostico.ColunasAsync(cs, "Pedidos", Ct)).Select(c => c.Nome).ShouldContain("Canal");
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorGo();
}
