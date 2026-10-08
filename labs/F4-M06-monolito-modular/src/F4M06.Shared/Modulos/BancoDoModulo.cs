using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace F4M06.Shared.Modulos;

/// <summary>Cria as tabelas de UM módulo (no schema dele), se ainda não existirem.</summary>
public interface IModuleDatabaseInitializer
{
    Task InicializarAsync(CancellationToken ct = default);
}

/// <summary>
/// Inicializador genérico: garante que o banco existe e cria as tabelas do <typeparamref name="TContext"/>
/// se o schema padrão dele ainda estiver vazio. Funciona com vários DbContexts no MESMO banco
/// (o <c>EnsureCreated</c> não serve para isso: ele não cria nada se o banco já tiver qualquer tabela).
/// Em produção, cada módulo teria as próprias migrations, com a tabela de histórico no próprio schema.
/// </summary>
public sealed class ModuleDatabaseInitializer<TContext>(TContext db) : IModuleDatabaseInitializer
    where TContext : DbContext
{
    public async Task InicializarAsync(CancellationToken ct = default)
    {
        var criador = db.Database.GetService<IRelationalDatabaseCreator>();
        if (!await criador.ExistsAsync(ct))
            await criador.CreateAsync(ct);

        var schema = db.Model.GetDefaultSchema() ?? "dbo";
        var tabelas = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.tables WHERE schema_id = SCHEMA_ID({schema})")
            .ToListAsync(ct);

        if (tabelas[0] == 0)
            await criador.CreateTablesAsync(ct);
    }
}

public static class BancoDoModuloExtensions
{
    /// <summary>Nome da connection string compartilhada: um banco, um schema por módulo.</summary>
    public const string ConnectionStringName = "OrderFlow";

    /// <summary>
    /// Registra o DbContext do módulo apontando para o banco do OrderFlow e o inicializador do schema dele.
    /// A connection string é lida na hora de montar as options (lambda com <see cref="IServiceProvider"/>),
    /// para que os testes possam sobrescrevê-la.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>((sp, options) =>
            options.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)));
        services.AddScoped<IModuleDatabaseInitializer, ModuleDatabaseInitializer<TContext>>();
        return services;
    }

    /// <summary>Roda o inicializador de cada módulo, um de cada vez (o primeiro cria o banco).</summary>
    public static async Task InicializarBancoDosModulosAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        foreach (var inicializador in scope.ServiceProvider.GetServices<IModuleDatabaseInitializer>())
            await inicializador.InicializarAsync(ct);
    }
}
