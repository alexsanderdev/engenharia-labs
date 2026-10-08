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
        where TDecorator : class, TServico =>
        throw new NotImplementedException(
            "TODO: ache o índice do último ServiceDescriptor de TServico (!IsKeyedService) e troque por " +
            "ServiceDescriptor.Describe(typeof(TServico), sp => ActivatorUtilities.CreateInstance<TDecorator>(sp, interno), original.Lifetime). " +
            "O interno vem de ImplementationInstance, ImplementationFactory(sp) ou ActivatorUtilities.GetServiceOrCreateInstance(sp, ImplementationType).");
}
