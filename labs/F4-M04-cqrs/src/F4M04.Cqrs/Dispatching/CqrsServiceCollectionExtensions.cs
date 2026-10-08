using System.Reflection;
using F4M04.Cqrs.Abstractions;
using F4M04.Cqrs.Pipeline;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace F4M04.Cqrs.Dispatching;

public static class CqrsServiceCollectionExtensions
{
    /// <summary>
    /// Ordem do pipeline, de FORA para DENTRO:
    /// commands: Logging → Validation → UnitOfWork → handler;
    /// queries:  Logging → Validation → handler (query não tem unidade de trabalho).
    /// </summary>
    private static readonly Type[] DecoratorsDeCommand =
        [typeof(LoggingCommandDecorator<,>), typeof(ValidationCommandDecorator<,>), typeof(UnitOfWorkCommandDecorator<,>)];

    private static readonly Type[] DecoratorsDeQuery =
        [typeof(LoggingQueryDecorator<,>), typeof(ValidationQueryDecorator<,>)];

    /// <summary>Varre os assemblies e registra dispatcher, handlers (já decorados), validators e handlers de eventos.</summary>
    public static IServiceCollection AddCqrs(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        return services.AddCqrs(assemblies.SelectMany(a => a.GetTypes()));
    }

    /// <summary>
    /// Mesmo que a sobrecarga por assembly, mas a partir de uma lista explícita de tipos
    /// (útil em testes). Regras:
    /// <list type="bullet">
    /// <item>só classes concretas e não genéricas abertas entram;</item>
    /// <item>cada <c>ICommandHandler&lt;,&gt;</c>/<c>IQueryHandler&lt;,&gt;</c> fechado é registrado como transient JÁ envolvido pelos decorators;</item>
    /// <item>dois handlers para a mesma mensagem → <see cref="InvalidOperationException"/>;</item>
    /// <item><c>IValidator&lt;T&gt;</c> e <c>IDomainEventHandler&lt;T&gt;</c> são registrados como transient (pode haver vários);</item>
    /// <item><see cref="IDispatcher"/> é registrado como scoped (uma vez só, mesmo chamando AddCqrs várias vezes).</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddCqrs(this IServiceCollection services, IEnumerable<Type> tipos)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(tipos);

        services.TryAddScoped<IDispatcher, Dispatcher>();

        var concretos = tipos.Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });
        foreach (var tipo in concretos)
        {
            foreach (var contrato in tipo.GetInterfaces().Where(i => i.IsGenericType))
            {
                var definicao = contrato.GetGenericTypeDefinition();

                if (definicao == typeof(ICommandHandler<,>))
                    RegistrarHandlerDecorado(services, contrato, tipo, DecoratorsDeCommand);
                else if (definicao == typeof(IQueryHandler<,>))
                    RegistrarHandlerDecorado(services, contrato, tipo, DecoratorsDeQuery);
                else if (definicao == typeof(IValidator<>) || definicao == typeof(IDomainEventHandler<>))
                    services.TryAddEnumerable(ServiceDescriptor.Transient(contrato, tipo));
            }
        }

        return services;
    }

    private static void RegistrarHandlerDecorado(IServiceCollection services, Type contrato, Type implementacao, Type[] decorators)
    {
        if (services.Any(d => d.ServiceType == contrato))
            throw new InvalidOperationException(
                $"Mais de um handler para {contrato.GenericTypeArguments[0].Name}: {implementacao.Name} é duplicado. Cada mensagem tem exatamente um handler.");

        var argumentos = contrato.GenericTypeArguments; // [TMensagem, TResult]
        services.TryAddTransient(implementacao);
        services.AddTransient(contrato, sp =>
        {
            // Monta de DENTRO para FORA: handler → último decorator → ... → primeiro decorator.
            object atual = sp.GetRequiredService(implementacao);
            for (var i = decorators.Length - 1; i >= 0; i--)
            {
                var decoratorFechado = decorators[i].MakeGenericType(argumentos);
                atual = ActivatorUtilities.CreateInstance(sp, decoratorFechado, atual);
            }
            return atual;
        });
    }
}
