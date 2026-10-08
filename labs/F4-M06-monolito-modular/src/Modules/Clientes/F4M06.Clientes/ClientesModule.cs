using F4M06.Clientes.Contracts;
using F4M06.Clientes.Endpoints;
using F4M06.Clientes.Fachada;
using F4M06.Clientes.Infra;
using F4M06.Shared.Modulos;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace F4M06.Clientes;

/// <summary>Módulo Clientes: cadastro e programa de fidelidade. Único tipo público do projeto.</summary>
public sealed class ClientesModule : IModule
{
    public string Nome => ClientesDbContext.Schema;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ClientesDbContext>();
        services.AddScoped<IClientesApi, ClientesApi>();

        // TODO (Passo 5): assinar o evento PedidoConfirmado (publicado por Pedidos) com o PedidoConfirmadoHandler.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapClientesEndpoints();
}
