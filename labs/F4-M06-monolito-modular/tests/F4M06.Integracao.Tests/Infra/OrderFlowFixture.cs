using F4M06.Shared.Modulos;
using Microsoft.Data.SqlClient;
using Respawn;
using Testcontainers.MsSql;

namespace F4M06.Integracao.Tests.Infra;

/// <summary>
/// JÁ VEM PRONTA. UM SQL Server em container para a coleção inteira; o banco e o schema de cada
/// módulo são criados UMA vez pelos inicializadores dos próprios módulos; o Respawn limpa os dados
/// dos schemas antes de cada teste.
/// </summary>
public sealed class OrderFlowFixture : IAsyncLifetime
{
    public const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string NomeDoBanco = "F4M06OrderFlow";

    /// <summary>Um schema por módulo (e o dbo, para limpar também o que cair lá por engano).</summary>
    public static readonly string[] Schemas = ["catalogo", "pedidos", "clientes", "dbo"];

    private MsSqlContainer? _container;
    private Respawner? _respawner;

    public string ConnectionString { get; private set; } = "";

    /// <summary>O Host de verdade (todos os módulos compostos), em memória, apontando para o container.</summary>
    public OrderFlowApiFactory Factory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _container = new MsSqlBuilder(Imagem).Build();
        await _container.StartAsync(TestContext.Current.CancellationToken);

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = NomeDoBanco,
        }.ConnectionString;

        Factory = new OrderFlowApiFactory(ConnectionString);

        // Cada módulo cria as PRÓPRIAS tabelas (no próprio schema) pelo IModuleDatabaseInitializer.
        await Factory.Services.InicializarBancoDosModulosAsync(TestContext.Current.CancellationToken);

        _respawner = await Respawner.CreateAsync(ConnectionString, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = Schemas,
        });
    }

    /// <summary>Apaga os dados de todas as tabelas (mantém o schema). Chamado antes de cada teste.</summary>
    public async Task ResetarBancoAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("A fixture não foi inicializada.");

        await _respawner.ResetAsync(ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (_container is not null) await _container.DisposeAsync();
    }
}

/// <summary>
/// Todos os testes de integração ficam nesta coleção: compartilham o container e rodam em série
/// (o Respawn limpa um banco compartilhado; em paralelo, um teste apagaria os dados do outro).
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoIntegracao : ICollectionFixture<OrderFlowFixture>
{
    public const string Nome = "Integração";
}
