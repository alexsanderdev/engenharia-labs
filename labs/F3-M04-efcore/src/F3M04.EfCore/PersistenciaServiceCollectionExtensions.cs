using Microsoft.Extensions.DependencyInjection;

namespace F3M04.EfCore;

public static class PersistenciaServiceCollectionExtensions
{
    /// <summary>
    /// Registra a persistência do OrderFlow: <see cref="Persistencia.OrderFlowDbContext"/> com SQL Server
    /// (scoped — um por requisição), <see cref="Pedidos.PedidoService"/> scoped e
    /// <see cref="TimeProvider.System"/> (sem sobrescrever um relógio já registrado).
    /// </summary>
    public static IServiceCollection AddOrderFlowPersistencia(this IServiceCollection services, string connectionString)
    {
        throw new NotImplementedException("TODO (Passo 9): AddDbContext<OrderFlowDbContext>(o => o.UseSqlServer(...)), TryAddSingleton(TimeProvider.System), AddScoped<PedidoService>().");
    }
}
