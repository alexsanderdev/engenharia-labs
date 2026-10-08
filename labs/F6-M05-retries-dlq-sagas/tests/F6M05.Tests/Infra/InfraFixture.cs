using F6M05.Tests.Infra;
using Microsoft.Data.SqlClient;
using RabbitMQ.Client;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

[assembly: AssemblyFixture(typeof(InfraFixture))]

namespace F6M05.Tests.Infra;

/// <summary>
/// PRONTA. Sobe, UMA vez para toda a assembly de testes e em paralelo, um RabbitMQ e um SQL
/// Server em containers (portas aleatórias), abre uma conexão AMQP compartilhada e cria o banco
/// da saga com <c>Sql/Esquema.sql</c> do projeto <c>src</c>.
/// </summary>
/// <remarks>
/// Isolamento: os testes de RabbitMQ usam nomes de fila únicos por teste (GUID), então rodam em
/// paralelo sem se ver. Os testes que usam o banco ficam na coleção <see cref="ColecaoSql"/>
/// (em série) e limpam as tabelas antes de cada teste: o verificador de prazos varre a tabela
/// inteira, e uma saga de outro teste rodando em paralelo seria concorrência NÃO controlada.
/// </remarks>
public sealed class InfraFixture : IAsyncLifetime
{
    public const string ImagemRabbit = "rabbitmq:4.1-management";
    public const string ImagemSql = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string NomeDoBanco = "F6M05_Sagas";

    private RabbitMqContainer? _rabbit;
    private MsSqlContainer? _sql;

    /// <summary>Conexão AMQP compartilhada (uma por processo, como em produção). Cada uso abre seus canais.</summary>
    public IConnection Rabbit { get; private set; } = null!;

    /// <summary>Connection string do banco da saga.</summary>
    public string Sql { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        _rabbit = new RabbitMqBuilder(ImagemRabbit).Build();
        _sql = new MsSqlBuilder(ImagemSql).Build();
        var ct = TestContext.Current.CancellationToken;
        await Task.WhenAll(_rabbit.StartAsync(ct), _sql.StartAsync(ct));

        var fabrica = new ConnectionFactory
        {
            Uri = new Uri(_rabbit.GetConnectionString()),
            ClientProvidedName = "F6M05.Tests",
        };
        Rabbit = await fabrica.CreateConnectionAsync(ct);

        var master = _sql.GetConnectionString();
        await ExecutarAsync(master, $"CREATE DATABASE {NomeDoBanco};");
        Sql = new SqlConnectionStringBuilder(master) { InitialCatalog = NomeDoBanco }.ConnectionString;

        var esquema = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "Esquema.sql"), ct);
        await ExecutarAsync(Sql, esquema);
    }

    /// <summary>Apaga todas as sagas (usado antes de cada teste da coleção SQL).</summary>
    public Task LimparSagasAsync() =>
        ExecutarAsync(Sql, "DELETE FROM dbo.SagaPedidoMensagem; DELETE FROM dbo.SagaPedido;");

    private static async Task ExecutarAsync(string connectionString, string sql)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao) { CommandTimeout = 60 };
        await comando.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Rabbit is not null) await Rabbit.DisposeAsync();
        if (_rabbit is not null) await _rabbit.DisposeAsync();
        if (_sql is not null) await _sql.DisposeAsync();
    }
}

/// <summary>Testes que usam o banco: em série, com as tabelas limpas antes de cada um.</summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoSql
{
    public const string Nome = "Saga no SQL Server";
}

/// <summary>PRONTA. Base dos testes da coleção SQL.</summary>
[Collection(ColecaoSql.Nome)]
public abstract class SqlTestBase(InfraFixture infra) : IAsyncLifetime
{
    protected InfraFixture Infra { get; } = infra;

    public virtual async ValueTask InitializeAsync() => await Infra.LimparSagasAsync();

    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
