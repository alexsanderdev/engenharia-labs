using F4M06.Pedidos.Endpoints;
using F4M06.Pedidos.Infra;
using F4M06.Shared.Modulos;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace F4M06.Pedidos;

/// <summary>Módulo Pedidos: criação e ciclo de vida de pedidos. Único tipo público do projeto.</summary>
public sealed class PedidosModule : IModule
{
    public string Nome => PedidosDbContext.Schema;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<PedidosDbContext>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapPedidosEndpoints();
}
