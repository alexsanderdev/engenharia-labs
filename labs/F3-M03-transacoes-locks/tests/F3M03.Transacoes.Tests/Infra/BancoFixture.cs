using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace F3M03.Transacoes.Tests.Infra;

/// <summary>
/// PRONTA. Sobe UM SQL Server em container para a assembly de testes e cria DOIS bancos com o
/// mesmo schema, diferentes só na configuração de isolamento:
/// <list type="bullet">
/// <item><see cref="Locking"/>: READ COMMITTED clássico (com locks de leitura) e ALLOW_SNAPSHOT_ISOLATION ON;</item>
/// <item><see cref="Rcsi"/>: READ_COMMITTED_SNAPSHOT ON (o padrão do Azure SQL Database).</item>
/// </list>
/// </summary>
/// <remarks>
/// As connection strings usam <c>Pooling=false</c>: cada <c>SqlConnection</c> é uma sessão nova, que
/// morre ao ser fechada (e com ela qualquer transação aberta e seus locks). Em produção você quer
/// pool; aqui queremos sessões previsíveis para observar locks. Todas as conexões dos testes e do
/// código do lab usam <c>Application Name=F3M03</c>; a infraestrutura usa outro nome, para que o
/// observador de bloqueios enxergue só as sessões do teste.
/// </remarks>
public sealed class BancoFixture : IAsyncLifetime
{
    public const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string NomeBancoLocking = "F3M03_Locking";
    public const string NomeBancoRcsi = "F3M03_Rcsi";
    public const string AppDosTestes = "F3M03";
    public const string AppDaInfra = "F3M03-Infra";

    private MsSqlContainer? _container;
    private string _master = "";

    /// <summary>Banco com READ COMMITTED "com locking" (padrão do SQL Server on-premises).</summary>
    public string Locking { get; private set; } = "";

    /// <summary>Banco com READ_COMMITTED_SNAPSHOT ON.</summary>
    public string Rcsi { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        _container = new MsSqlBuilder(Imagem).Build();
        await _container.StartAsync(TestContext.Current.CancellationToken);
        _master = Montar(_container.GetConnectionString(), "master", AppDaInfra);

        Locking = Montar(_master, NomeBancoLocking, AppDosTestes);
        Rcsi = Montar(_master, NomeBancoRcsi, AppDosTestes);

        await ExecutarAsync(_master,
            $"""
            CREATE DATABASE {NomeBancoLocking};
            ALTER DATABASE {NomeBancoLocking} SET ALLOW_SNAPSHOT_ISOLATION ON;
            ALTER DATABASE {NomeBancoLocking} SET READ_COMMITTED_SNAPSHOT OFF;
            CREATE DATABASE {NomeBancoRcsi};
            ALTER DATABASE {NomeBancoRcsi} SET ALLOW_SNAPSHOT_ISOLATION ON;
            ALTER DATABASE {NomeBancoRcsi} SET READ_COMMITTED_SNAPSHOT ON;
            """);

        foreach (var banco in new[] { Locking, Rcsi })
            await ExecutarAsync(ComApp(banco, AppDaInfra), Esquema.CriarTabelas);
    }

    /// <summary>
    /// Antes de cada teste: derruba sessões que um teste anterior possa ter deixado presas
    /// (ex.: falhou no meio com uma transação aberta) e recarrega os dados conhecidos.
    /// </summary>
    public async Task ResetarAsync()
    {
        await ExecutarAsync(_master,
            $"""
            DECLARE @sql nvarchar(max) = N'';
            SELECT @sql += N'KILL ' + CAST(session_id AS nvarchar(10)) + N';'
              FROM sys.dm_exec_sessions
             WHERE program_name = N'{AppDosTestes}' AND session_id <> @@SPID;
            EXEC (@sql);
            """);

        foreach (var banco in new[] { Locking, Rcsi })
            await ExecutarAsync(ComApp(banco, AppDaInfra), Esquema.ResetarDados);
    }

    /// <summary>Connection string de infraestrutura (não aparece para o observador de bloqueios).</summary>
    public static string ComApp(string connectionString, string app) =>
        new SqlConnectionStringBuilder(connectionString) { ApplicationName = app }.ConnectionString;

    private static string Montar(string baseCs, string banco, string app) =>
        new SqlConnectionStringBuilder(baseCs)
        {
            InitialCatalog = banco,
            ApplicationName = app,
            Pooling = false,
            ConnectTimeout = 15,
        }.ConnectionString;

    private static async Task ExecutarAsync(string connectionString, string sql)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao) { CommandTimeout = 60 };
        await comando.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}

/// <summary>
/// Todos os testes que usam o banco ficam nesta coleção: compartilham o container e rodam em
/// SÉRIE. Testes de concorrência criam a concorrência que querem, de forma controlada; um teste
/// vizinho rodando em paralelo seria concorrência NÃO controlada (e teste intermitente).
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoBanco : ICollectionFixture<BancoFixture>
{
    public const string Nome = "SQL Server";
}
