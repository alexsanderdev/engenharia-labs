using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace F4M04.Cqrs.Dispatching;

public static class CqrsServiceCollectionExtensions
{
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
    /// <item><see cref="Abstractions.IDispatcher"/> é registrado como scoped (uma vez só, mesmo chamando AddCqrs várias vezes).</item>
    /// </list>
    /// Ordem do pipeline, de FORA para DENTRO:
    /// commands: Logging → Validation → UnitOfWork → handler;
    /// queries:  Logging → Validation → handler (query não tem unidade de trabalho).
    /// </summary>
    public static IServiceCollection AddCqrs(this IServiceCollection services, IEnumerable<Type> tipos) =>
        throw new NotImplementedException(
            "TODO: registre IDispatcher (TryAddScoped), percorra os tipos concretos e, para cada interface genérica fechada, " +
            "registre handlers decorados (factory que monta handler → decorators de dentro para fora com ActivatorUtilities), " +
            "validators e handlers de eventos (TryAddEnumerable). Handler duplicado → InvalidOperationException.");
}
