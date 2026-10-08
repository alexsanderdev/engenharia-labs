using Microsoft.Extensions.DependencyInjection;

namespace F4M07.Patterns.Precos;

/// <summary>
/// O container da Microsoft não tem "Decorate" nativo (o Scrutor tem). Aqui você escreve o seu,
/// em ~20 linhas — e entende de vez como um decorator é montado pelo DI.
/// </summary>
public static class DecoracaoDeServicos
{
    /// <summary>
    /// Envolve o ÚLTIMO registro (não keyed) de <typeparamref name="TServico"/> com <typeparamref name="TDecorator"/>,
    /// mantendo o MESMO lifetime. O decorator é criado por <see cref="ActivatorUtilities"/>: o serviço interno é
    /// passado explicitamente e as demais dependências (logger, TimeProvider, options...) vêm do container.
    /// Chamadas encadeadas empilham: o último <c>Decorar</c> fica por fora.
    /// Sem registro prévio de <typeparamref name="TServico"/>: <see cref="InvalidOperationException"/>.
    /// </summary>
    public static IServiceCollection Decorar<TServico, TDecorator>(this IServiceCollection services)
        where TServico : class
        where TDecorator : class, TServico
    {
        var indice = -1;
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(TServico) && !services[i].IsKeyedService)
            {
                indice = i;
                break;
            }
        }

        if (indice < 0)
            throw new InvalidOperationException(
                $"Não há registro de {typeof(TServico).Name} para decorar com {typeof(TDecorator).Name}. Registre o serviço antes.");

        var original = services[indice];
        services[indice] = ServiceDescriptor.Describe(
            typeof(TServico),
            sp => ActivatorUtilities.CreateInstance<TDecorator>(sp, CriarInterno(sp, original)),
            original.Lifetime);

        return services;
    }

    private static object CriarInterno(IServiceProvider sp, ServiceDescriptor original) =>
        original.ImplementationInstance
        ?? original.ImplementationFactory?.Invoke(sp)
        ?? ActivatorUtilities.GetServiceOrCreateInstance(sp, original.ImplementationType!);
}
