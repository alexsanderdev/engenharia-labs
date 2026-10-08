using System.Diagnostics;
using F3M02.Indices;
using F3M02.Indices.Tests.Infra;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

// Um container por assembly de teste: todas as classes recebem a MESMA fixture.
[assembly: AssemblyFixture(typeof(SqlServerFixture))]

namespace F3M02.Indices.Tests.Infra;

/// <summary>
/// Infra PRONTA. Sobe UM SQL Server 2022 em container, cria o banco <c>F3M02OrderFlow</c> com
/// 200 mil pedidos (<c>Infra/BaseOrderFlow.sql</c>) e, na primeira vez que um teste pede,
/// aplica o SEU <c>Sql/Indices.sql</c>. Os testes só leem: não há o que isolar entre eles.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string NomeDoBanco = "F3M02OrderFlow";

    private MsSqlContainer? _container;
    private readonly Lazy<Task> _indices;

    public SqlServerFixture() =>
        _indices = new Lazy<Task>(AplicarIndicesAsync, LazyThreadSafetyMode.ExecutionAndPublication);

    public string ConnectionString { get; private set; } = "";

    /// <summary>Quanto tempo levou a carga da massa (aparece no teste de sanidade).</summary>
    public TimeSpan TempoDeCarga { get; private set; }

    public async ValueTask InitializeAsync()
    {
        _container = new MsSqlBuilder(Imagem).Build();
        await _container.StartAsync();

        await using (var master = new SqlConnection(_container.GetConnectionString()))
        {
            await master.OpenAsync();
            // Collation SQL (padrão de instalação do SQL Server): é nela que varchar x nvarchar vira scan.
            await using var cmd = new SqlCommand(
                $"""
                CREATE DATABASE [{NomeDoBanco}] COLLATE SQL_Latin1_General_CP1_CI_AS;
                ALTER DATABASE [{NomeDoBanco}] SET RECOVERY SIMPLE;
                """, master);
            await cmd.ExecuteNonQueryAsync();
        }

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = NomeDoBanco,
        }.ConnectionString;

        var cronometro = Stopwatch.StartNew();
        await ExecutarScriptAsync(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Infra", "BaseOrderFlow.sql")));
        TempoDeCarga = cronometro.Elapsed;
    }

    /// <summary>
    /// Garante que o seu <c>Indices.sql</c> foi aplicado (uma vez só, mesmo com testes em paralelo).
    /// Se o script tiver erro, todos os testes que dependem dele mostram a mensagem do SQL Server.
    /// </summary>
    public Task GarantirIndicesAsync() => _indices.Value;

    private async Task AplicarIndicesAsync()
    {
        try
        {
            await ExecutarScriptAsync(ScriptsSql.Ler("Indices.sql"));
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"O SQL Server recusou o seu Sql/Indices.sql (linha {ex.LineNumber} do lote): {ex.Message}", ex);
        }

        // Planos antigos (compilados antes dos índices) não devem atrapalhar a medição.
        await ExecutarScriptAsync("DBCC FREEPROCCACHE WITH NO_INFOMSGS;");
    }

    private async Task ExecutarScriptAsync(string script)
    {
        await using var conexao = new SqlConnection(ConnectionString);
        await conexao.OpenAsync();
        foreach (var lote in ScriptsSql.DividirEmLotes(script))
        {
            await using var cmd = new SqlCommand(lote, conexao) { CommandTimeout = 300 };
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}
