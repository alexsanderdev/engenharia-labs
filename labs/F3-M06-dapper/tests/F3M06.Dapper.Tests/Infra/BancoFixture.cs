using F3M06.Dapper.Escrita;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(F3M06.Dapper.Tests.Infra.BancoFixture))]

namespace F3M06.Dapper.Tests.Infra;

/// <summary>
/// Já vem pronta (o foco do lab é Dapper, não Testcontainers).
/// UM container de SQL Server para o assembly inteiro: schema criado pelo EF Core (lado de escrita)
/// e massa de dados gravada uma vez. Os testes deste lab só LEEM, então não precisam de reset
/// entre testes e podem rodar em paralelo.
/// </summary>
public sealed class BancoFixture : IAsyncLifetime
{
    public const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string NomeDoBanco = "F3M06Loja";

    private MsSqlContainer? _container;

    public string ConnectionString { get; private set; } = "";

    public DadosDeTeste Dados { get; } = new();

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        _container = new MsSqlBuilder(Imagem).Build();
        await _container.StartAsync(ct);

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = NomeDoBanco,
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<LojaDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new LojaDbContext(options);
        await db.Database.EnsureCreatedAsync(ct);

        db.Clientes.AddRange(Dados.Clientes);
        db.Produtos.AddRange(Dados.Produtos);
        db.Pedidos.AddRange(Dados.Pedidos);
        await db.SaveChangesAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}
