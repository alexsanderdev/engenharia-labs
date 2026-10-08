using F3M07.Dados.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(F3M07.Dados.Tests.Infra.SqlServerFixture))]

namespace F3M07.Dados.Tests.Infra;

/// <summary>
/// Já vem pronta. UM container de SQL Server para o assembly inteiro.
/// <list type="bullet">
/// <item>Testes de migration criam bancos NOVOS e vazios (um por teste) — <see cref="NovoBanco"/>.</item>
/// <item>Testes de concorrência e paginação usam o "banco principal", criado UMA vez com
/// <c>Database.MigrateAsync()</c> (ou seja: com as SUAS migrations) e limpo pelo Respawn antes de cada teste.</item>
/// </list>
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string BancoPrincipal = "F3M07Loja";

    private MsSqlContainer? _container;
    private readonly SemaphoreSlim _travaBancoPrincipal = new(1, 1);
    private Respawner? _respawner;

    private string _connectionStringMaster = "";

    public async ValueTask InitializeAsync()
    {
        _container = new MsSqlBuilder(Imagem).Build();
        await _container.StartAsync(TestContext.Current.CancellationToken);
        _connectionStringMaster = _container.GetConnectionString();
    }

    /// <summary>Connection string para um banco do container (o banco não precisa existir: MigrateAsync cria).</summary>
    public string ConnectionStringPara(string banco) =>
        new SqlConnectionStringBuilder(_connectionStringMaster) { InitialCatalog = banco }.ConnectionString;

    /// <summary>Nome único para um banco descartável (testes de migration rodam em paralelo).</summary>
    public static string NovoBanco(string prefixo) => $"{prefixo}_{Guid.NewGuid():N}";

    public static DbContextOptions<LojaDbContext> Options(string connectionString, params IInterceptor[] interceptors) =>
        new DbContextOptionsBuilder<LojaDbContext>()
            .UseSqlServer(connectionString)
            .AddInterceptors(interceptors)
            .Options;

    /// <summary>Cria o banco principal com as migrations (uma vez) e limpa os dados antes de cada teste.</summary>
    public async Task PrepararBancoPrincipalAsync(CancellationToken ct)
    {
        var connectionString = ConnectionStringPara(BancoPrincipal);

        await _travaBancoPrincipal.WaitAsync(ct);
        try
        {
            if (_respawner is null)
            {
                await using (var db = new LojaDbContext(Options(connectionString)))
                    await db.Database.MigrateAsync(ct);

                _respawner = await Respawner.CreateAsync(connectionString, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.SqlServer,
                    SchemasToInclude = ["dbo"],
                    TablesToIgnore = [new Table("__EFMigrationsHistory")],
                });
            }

            await _respawner.ResetAsync(connectionString);
        }
        finally
        {
            _travaBancoPrincipal.Release();
        }
    }

    /// <summary>Cria um banco vazio (sem tabelas) — para executar o script idempotente.</summary>
    public async Task CriarBancoVazioAsync(string banco, CancellationToken ct)
    {
        await using var conexao = new SqlConnection(_connectionStringMaster);
        await conexao.OpenAsync(ct);
        await using var comando = new SqlCommand($"CREATE DATABASE [{banco}]", conexao);
        await comando.ExecuteNonQueryAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        _travaBancoPrincipal.Dispose();
        if (_container is not null) await _container.DisposeAsync();
    }
}

/// <summary>
/// Testes que usam o banco principal ficam nesta coleção: rodam em série entre si
/// (o Respawn limpa um banco compartilhado). Os testes de migration, em bancos próprios,
/// rodam em paralelo com eles.
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoBancoPrincipal
{
    public const string Nome = "Banco principal";
}
