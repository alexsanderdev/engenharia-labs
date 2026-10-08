using F4M04.Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace F4M04.Cqrs.Pedidos.Infra;

/// <summary>
/// Publicador em processo (PRONTO): resolve TODOS os <see cref="IDomainEventHandler{TEvent}"/>
/// registrados para o tipo concreto do evento e chama um por um.
/// Repare no tipo genérico fechado em tempo de execução (MakeGenericType): o seu Dispatcher
/// vai precisar do mesmo truque, mas sem reflexão a cada chamada.
/// </summary>
public sealed class DomainEventPublisher(IServiceProvider provider) : IDomainEventPublisher
{
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var tipoHandler = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var metodo = tipoHandler.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

        foreach (var handler in provider.GetServices(tipoHandler))
            await ((Task)metodo.Invoke(handler, [domainEvent, ct])!).ConfigureAwait(false);
    }
}
