using System.Reflection;
using FluentValidation;

namespace F4M05.Api.Comum;

/// <summary>
/// Descobre as fatias por reflexão: handlers, validadores e endpoints.
/// Criar uma feature nova = criar UM arquivo em Features/. Nada de editar Program.cs ou um "PedidoService".
/// </summary>
public static class RegistroDeFeatures
{
    public static IServiceCollection AddFeatures(this IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        var concretos = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false });

        foreach (var tipo in concretos)
        {
            foreach (var contrato in tipo.GetInterfaces().Where(i => i.IsGenericType))
            {
                var definicao = contrato.GetGenericTypeDefinition();

                if (definicao == typeof(IQueryHandler<,>))
                {
                    services.AddScoped(contrato, tipo);
                }
                else if (definicao == typeof(ICommandHandler<,>))
                {
                    // Handler concreto + decorator de validação "por fora".
                    services.AddScoped(tipo);
                    var decorator = typeof(ValidacaoCommandHandlerDecorator<,>).MakeGenericType(contrato.GetGenericArguments());
                    services.AddScoped(contrato, sp => ActivatorUtilities.CreateInstance(sp, decorator, sp.GetRequiredService(tipo)));
                }
            }

            if (typeof(IEndpoint).IsAssignableFrom(tipo))
                services.AddSingleton(typeof(IEndpoint), tipo);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapFeatures(this IEndpointRouteBuilder app)
    {
        foreach (var endpoint in app.ServiceProvider.GetServices<IEndpoint>())
            endpoint.MapEndpoint(app);
        return app;
    }
}
