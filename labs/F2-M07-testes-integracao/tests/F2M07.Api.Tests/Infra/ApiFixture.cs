using F2M07.Api.Infrastructure;
using Microsoft.Data.SqlClient;
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

#pragma warning disable CS0649 // TODO (Passo 1): o campo é preenchido em SubirContainerAsync (apague este pragma).
    private MsSqlContainer? _container;
#pragma warning restore CS0649

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
    private Task SubirContainerAsync() =>
        throw new NotImplementedException(
            "TODO (Passo 1): crie o container com new MsSqlBuilder(Imagem).Build(), guarde em _container, " +
            "chame StartAsync e monte ConnectionString a partir de _container.GetConnectionString() " +
            "trocando o InitialCatalog para NomeDoBanco (use SqlConnectionStringBuilder).");

    /// <summary>
    /// Passo 2: cria o banco e as tabelas UMA vez, usando o DbContext da própria API
    /// (se o override da connection string não funcionou, falha aqui).
    /// </summary>
    private static Task CriarSchemaAsync()
    {
        // TODO (Passo 2): use ComBancoAsync para chamar db.Database.EnsureCreatedAsync().
        return Task.CompletedTask;
    }

    /// <summary>
    /// Passo 3: cria o <see cref="Respawner"/>, que lê o grafo de FKs uma vez e depois
    /// apaga os dados na ordem certa a cada reset.
    /// </summary>
    private static Task PrepararRespawnAsync()
    {
        // TODO (Passo 3): crie um campo "Respawner? _respawner" e preencha com
        // Respawner.CreateAsync(ConnectionString, new RespawnerOptions { DbAdapter = DbAdapter.SqlServer, ... }).
        return Task.CompletedTask;
    }

    /// <summary>Passo 3: apaga os dados de todas as tabelas (mantém o schema). Chamado antes de cada teste.</summary>
    public Task ResetarBancoAsync()
    {
        // TODO (Passo 3): chame _respawner.ResetAsync(ConnectionString).
        // Enquanto isto não for feito, os dados de um teste vazam para o próximo.
        _ = ConnectionString;
        return Task.CompletedTask;
    }

    /// <summary>Passo 2: executa uma ação com um DbContext novo (escopo próprio), fora do pipeline HTTP.</summary>
    public Task ComBancoAsync(Func<PedidosDbContext, Task> acao) =>
        throw new NotImplementedException(
            "TODO (Passo 2): crie um escopo com Factory.Services.CreateAsyncScope(), resolva o " +
            "PedidosDbContext do escopo e execute a ação.");

    /// <summary>Passo 2: executa uma consulta com um DbContext novo e devolve o resultado.</summary>
    public Task<T> ComBancoAsync<T>(Func<PedidosDbContext, Task<T>> consulta) =>
        throw new NotImplementedException(
            "TODO (Passo 2): igual ao ComBancoAsync acima, devolvendo o resultado da consulta.");

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
