using F3M01.Sql;
using F3M01.Sql.Tests.Infra;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

// Um container por assembly de teste: todas as classes recebem a MESMA fixture.
[assembly: AssemblyFixture(typeof(SqlServerFixture))]

namespace F3M01.Sql.Tests.Infra;

/// <summary>
/// Infra PRONTA (o foco deste lab é SQL, não Testcontainers). Sobe UM SQL Server 2022 em container
/// e prepara dois bancos:
/// <list type="bullet">
/// <item><c>F3M01Consultas</c>: schema simplificado + massa de dados da Parte B (criado na inicialização).</item>
/// <item><c>F3M01Modelagem</c>: banco vazio onde o SEU <c>Sql/ParteA/Schema.sql</c> é executado uma vez,
/// na primeira vez que um teste da Parte A pede (assim um erro no seu DDL não derruba a Parte B).</item>
/// </list>
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    /// <summary>Imagem fixa: a mesma no seu PC e no CI.</summary>
    public const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";

    private MsSqlContainer? _container;
    private readonly Lazy<Task<string>> _modelagem;

    public SqlServerFixture() =>
        _modelagem = new Lazy<Task<string>>(CriarBancoDaModelagemAsync, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Connection string do banco da Parte B (consultas), já com dados.</summary>
    public string ConsultasConnectionString { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        _container = new MsSqlBuilder(Imagem).Build();
        await _container.StartAsync();

        ConsultasConnectionString = await CriarBancoAsync("F3M01Consultas");
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Infra", "BaseConsultas.sql"));
        await ExecutarScriptAsync(ConsultasConnectionString, script);
    }

    /// <summary>
    /// Connection string do banco da Parte A, com o seu <c>Schema.sql</c> aplicado (uma vez só).
    /// Se o script tiver erro, todos os testes da Parte A mostram a mensagem do SQL Server.
    /// </summary>
    public Task<string> BancoDaModelagemAsync() => _modelagem.Value;

    private async Task<string> CriarBancoDaModelagemAsync()
    {
        var connectionString = await CriarBancoAsync("F3M01Modelagem");
        try
        {
            await ExecutarScriptAsync(connectionString, ScriptsSql.Ler("ParteA/Schema.sql"));
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"O SQL Server recusou o seu Sql/ParteA/Schema.sql (linha {ex.LineNumber} do lote): {ex.Message}", ex);
        }
        return connectionString;
    }

    private async Task<string> CriarBancoAsync(string nome)
    {
        await using (var master = new SqlConnection(_container!.GetConnectionString()))
        {
            await master.OpenAsync();
            await using var cmd = new SqlCommand($"CREATE DATABASE [{nome}];", master);
            await cmd.ExecuteNonQueryAsync();
        }

        return new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = nome }.ConnectionString;
    }

    /// <summary>Executa um script lote a lote (separados por <c>GO</c>).</summary>
    public static async Task ExecutarScriptAsync(string connectionString, string script)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        foreach (var lote in ScriptsSql.DividirEmLotes(script))
        {
            await using var cmd = new SqlCommand(lote, conexao);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}
