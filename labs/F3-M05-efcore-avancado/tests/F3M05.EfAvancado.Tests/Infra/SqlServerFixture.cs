using F3M05.EfAvancado.Persistencia;
using F3M05.EfAvancado.Persistencia.Interceptadores;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.MsSql;

namespace F3M05.EfAvancado.Tests.Infra;

/// <summary>
/// JÁ VEM PRONTA. Um SQL Server em container para a coleção "Banco"; schema criado UMA vez a
/// partir do modelo (<c>EnsureCreated</c>). Guarda as peças compartilhadas pelos testes:
/// relógio, captura de SQL e os interceptadores (as mesmas instâncias em todos os contextos,
/// para o EF reaproveitar o service provider interno).
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string NomeDoBanco = "F3M05Loja";

    private MsSqlContainer? _container;

    public string ConnectionString { get; private set; } = "";

    /// <summary>Relógio compartilhado. Só anda para frente: os testes usam tempos RELATIVOS a ele.</summary>
    public FakeTimeProvider Relogio { get; } = new(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Todos os comandos SQL enviados pelos contextos dos testes.</summary>
    public CapturaDeSql Sql { get; } = new();

    public AuditoriaInterceptor Auditoria { get; private set; } = null!;
    public ExclusaoLogicaInterceptor ExclusaoLogica { get; } = new();

    public async ValueTask InitializeAsync()
    {
        Auditoria = new AuditoriaInterceptor(Relogio);

        _container = new MsSqlBuilder(Imagem).Build();
        await _container.StartAsync(TestContext.Current.CancellationToken);

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = NomeDoBanco,
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<LojaDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new LojaDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}

[CollectionDefinition(Nome)]
public sealed class ColecaoBanco : ICollectionFixture<SqlServerFixture>
{
    public const string Nome = "Banco";
}
