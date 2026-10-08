using F3M04.EfCore.Pedidos;
using F3M04.EfCore.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace F3M04.EfCore;

public static class PersistenciaServiceCollectionExtensions
{
    /// <summary>
    /// Registra a persistência do OrderFlow: <see cref="OrderFlowDbContext"/> com SQL Server
    /// (scoped — um por requisição), <see cref="PedidoService"/> scoped e
    /// <see cref="TimeProvider.System"/> (sem sobrescrever um relógio já registrado).
    /// </summary>
    public static IServiceCollection AddOrderFlowPersistencia(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<OrderFlowDbContext>(options => options.UseSqlServer(connectionString));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<PedidoService>();
        return services;
    }
}
