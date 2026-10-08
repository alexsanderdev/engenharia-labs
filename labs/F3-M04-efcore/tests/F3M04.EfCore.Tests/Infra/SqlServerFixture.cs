using F3M04.EfCore.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace F3M04.EfCore.Tests.Infra;

/// <summary>
/// JÁ VEM PRONTA. Sobe UM SQL Server em container para todos os testes da coleção "Banco"
/// e cria o schema UMA vez a partir do SEU modelo (<see cref="OrderFlowDbContext"/>) com
/// <c>EnsureCreated</c> — por isso o schema reflete exatamente o que você configurou.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    /// <summary>Imagem fixa: a mesma no seu PC e no CI.</summary>
    public const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";

    public const string NomeDoBanco = "F3M04OrderFlow";

    private MsSqlContainer? _container;

    /// <summary>Connection string apontando para <see cref="NomeDoBanco"/> no container.</summary>
    public string ConnectionString { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        _container = new MsSqlBuilder(Imagem).Build();
        await _container.StartAsync(TestContext.Current.CancellationToken);

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = NomeDoBanco,
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<OrderFlowDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new OrderFlowDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}

/// <summary>
/// Todos os testes que tocam o banco ficam nesta coleção: compartilham o container e rodam
/// em série (cada teste roda dentro de uma transação que é desfeita no fim).
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoBanco : ICollectionFixture<SqlServerFixture>
{
    public const string Nome = "Banco";
}
