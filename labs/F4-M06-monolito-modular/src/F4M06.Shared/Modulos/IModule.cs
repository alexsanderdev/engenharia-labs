using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace F4M06.Shared.Modulos;

/// <summary>
/// Ponto de entrada de um módulo. É o ÚNICO tipo público do projeto de implementação:
/// o Host conhece o módulo por aqui e nada mais (entidades, DbContext, handlers e
/// endpoints ficam <c>internal</c>).
/// </summary>
public interface IModule
{
    /// <summary>Nome do módulo (ex.: "catalogo"). Por convenção, é também o nome do schema dele no SQL Server.</summary>
    string Nome { get; }

    /// <summary>
    /// Registra os serviços do módulo: DbContext próprio, implementação dos contratos
    /// públicos (<c>.Contracts</c>) e assinaturas de eventos de integração.
    /// </summary>
    void Register(IServiceCollection services, IConfiguration configuration);

    /// <summary>Mapeia os endpoints HTTP do módulo (cada módulo tem o próprio prefixo de rota).</summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
