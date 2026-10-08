using F4M04.Cqrs.Abstractions;
using F4M04.Cqrs.Pedidos.Leitura;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace F4M04.Cqrs.Pedidos.Infra;

public static class PedidosServiceCollectionExtensions
{
    /// <summary>
    /// Infraestrutura em memória do módulo Pedidos (PRONTA): bancos de escrita e de leitura,
    /// repositório, unidade de trabalho e publicador de eventos. Handlers, validators e
    /// projeções NÃO são registrados aqui: isso é trabalho do <c>AddCqrs</c> (varredura).
    /// </summary>
    public static IServiceCollection AddPedidosEmMemoria(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<BancoDeEscrita>();
        services.TryAddSingleton<BancoDeLeitura>();
        services.TryAddScoped<RepositorioDePedidos>();
        services.TryAddScoped<IRepositorioDePedidos>(sp => sp.GetRequiredService<RepositorioDePedidos>());
        services.TryAddScoped<ICatalogo>(sp => sp.GetRequiredService<RepositorioDePedidos>());
        services.TryAddScoped<IUnitOfWork, UnitOfWorkEmMemoria>();
        services.TryAddScoped<IDomainEventPublisher, DomainEventPublisher>();
        return services;
    }
}
