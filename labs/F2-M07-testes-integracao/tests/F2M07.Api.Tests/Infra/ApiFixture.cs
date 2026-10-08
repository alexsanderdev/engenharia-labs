using F2M07.Api.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Respawn;
using Testcontainers.MsSql;

namespace F2M07.Api.Tests.Infra;

/// <summary>
/// Fixture compartilhada por TODOS os testes da coleção "Integração":
/// sobe UM container de SQL Server, cria o schema uma vez e prepara o Respawn.
/// Cada teste só limpa os dados (rápido) em vez de recriar o banco (lento).
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    /// <summary>Imagem fixa: a mesma no seu PC e no GitHub Actions.</summary>
    public const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";

    /// <summary>Nome do banco de teste criado dentro do container.</summary>
    public const string NomeDoBanco = "F2M07Pedidos";

    private MsSqlContainer? _container;
    private Respawner? _respawner;

    /// <summary>Connection string apontando para <see cref="NomeDoBanco"/> no container.</summary>
    public string ConnectionString { get; private set; } = "";

    /// <summary>Relógio controlado: a API usa este TimeProvider no lugar do relógio do sistema.</summary>
    public FakeTimeProvider Relogio { get; } = new(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero));

    /// <summary>A API de verdade, em memória, apontando para o banco do container.</summary>
    public PedidosApiFactory Factory { get; private set; } = null!;

    /// <summary>Roda UMA vez para a coleção inteira (não uma vez por teste).</summary>
    public async ValueTask InitializeAsync()
    {
        await SubirContainerAsync();                                // Passo 1
        Factory = new PedidosApiFactory(ConnectionString, Relogio); // Passos 2 e 4 (ver a factory)
        await CriarSchemaAsync();                                   // Passo 2
        await PrepararRespawnAsync();                               // Passo 3
    }

    /// <summary>
    /// Passo 1: cria e inicia o container com a imagem <see cref="Imagem"/> e preenche
    /// <see cref="ConnectionString"/> apontando para o banco <see cref="NomeDoBanco"/>.
    /// </summary>
    private async Task SubirContainerAsync()
    {
        _container = new MsSqlBuilder(Imagem).Build();
        await _container.StartAsync(TestContext.Current.CancellationToken);

        // A connection string do container aponta para "master"; usamos um banco próprio.
        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = NomeDoBanco,
        }.ConnectionString;
    }

    /// <summary>
    /// Passo 2: cria o banco e as tabelas UMA vez, usando o DbContext da própria API
    /// (se o override da connection string não funcionou, falha aqui).
    /// </summary>
    private Task CriarSchemaAsync() =>
        ComBancoAsync(async db => { await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken); });

    /// <summary>
    /// Passo 3: cria o <see cref="Respawner"/>, que lê o grafo de FKs uma vez e depois
    /// apaga os dados na ordem certa a cada reset.
    /// </summary>
    private async Task PrepararRespawnAsync()
    {
        _respawner = await Respawner.CreateAsync(ConnectionString, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
        });
    }

    /// <summary>Passo 3: apaga os dados de todas as tabelas (mantém o schema). Chamado antes de cada teste.</summary>
    public async Task ResetarBancoAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException("A fixture não foi inicializada.");

        await _respawner.ResetAsync(ConnectionString);
    }

    /// <summary>Passo 2: executa uma ação com um DbContext novo (escopo próprio), fora do pipeline HTTP.</summary>
    public async Task ComBancoAsync(Func<PedidosDbContext, Task> acao)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
        await acao(db);
    }

    /// <summary>Passo 2: executa uma consulta com um DbContext novo e devolve o resultado.</summary>
    public async Task<T> ComBancoAsync<T>(Func<PedidosDbContext, Task<T>> consulta)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
        return await consulta(db);
    }

    public async ValueTask DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (_container is not null) await _container.DisposeAsync();
    }
}

/// <summary>
/// Todos os testes que usam o banco ficam nesta coleção: compartilham a fixture e rodam
/// em série (o Respawn limpa um banco compartilhado, então testes em paralelo se atropelariam).
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoIntegracao : ICollectionFixture<ApiFixture>
{
    public const string Nome = "Integração";
}
