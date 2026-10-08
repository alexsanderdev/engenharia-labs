using F4M01.Application.Abstracoes;
using F4M01.Infrastructure.Persistencia;
using F4M01.Infrastructure.Tempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace F4M01.Infrastructure;

/// <summary>
/// Ponto único onde a Infrastructure se registra. A Api chama isto do composition root
/// e não precisa saber que existe EF Core, SQL Server ou quais classes implementam as portas.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<OrderFlowDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IPedidoRepository, PedidoRepositoryEf>();
        services.AddScoped<IProdutoRepository, ProdutoRepositoryEf>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IRelogio, RelogioDoSistema>();
        return services;
    }
}
