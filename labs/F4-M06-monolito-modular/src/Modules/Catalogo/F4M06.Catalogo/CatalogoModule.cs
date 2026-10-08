using F4M06.Catalogo.Contracts;
using F4M06.Catalogo.Endpoints;
using F4M06.Catalogo.Fachada;
using F4M06.Catalogo.Infra;
using F4M06.Shared.Modulos;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace F4M06.Catalogo;

/// <summary>Módulo Catálogo: produtos, preços e disponibilidade. Único tipo público do projeto.</summary>
public sealed class CatalogoModule : IModule
{
    public string Nome => CatalogoDbContext.Schema;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<CatalogoDbContext>();

        // Contrato público síncrono: outros módulos pedem ICatalogoApi e recebem a implementação interna.
        services.AddScoped<ICatalogoApi, CatalogoApi>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapProdutosEndpoints();
}
