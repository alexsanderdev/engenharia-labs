using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace F4M06.Shared.Eventos;

/// <summary>Registro de uma assinatura: "o handler X reage ao evento Y".</summary>
public sealed record IntegrationEventSubscription(Type EventType, Type HandlerType);

/// <summary>
/// Barramento de eventos EM PROCESSO. Para cada assinante do evento:
/// <list type="bullet">
/// <item>cria um escopo de DI NOVO (o assinante tem o próprio DbContext e a própria transação,
/// nunca a do publicador);</item>
/// <item>isola falhas: um assinante com erro é logado e não derruba quem publicou nem os outros assinantes.</item>
/// </list>
/// Limitação consciente: se o processo cair entre o commit do publicador e o fim da entrega, o evento se perde
/// (não há persistência). A solução para isso é o Outbox (Fase 6). O assinante deve ser idempotente.
/// </summary>
internal sealed partial class InProcessEventBus(
    IServiceScopeFactory scopes,
    IEnumerable<IntegrationEventSubscription> assinaturas,
    ILogger<InProcessEventBus> logger) : IEventBus
{
    private readonly IntegrationEventSubscription[] _assinaturas = [.. assinaturas];

    public async Task PublishAsync<TEvent>(TEvent evento, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(evento);

        foreach (var assinatura in _assinaturas.Where(a => a.EventType == typeof(TEvent)))
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = (IIntegrationEventHandler<TEvent>)scope.ServiceProvider.GetRequiredService(assinatura.HandlerType);
            try
            {
                await handler.HandleAsync(evento, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogFalhaNoAssinante(ex, typeof(TEvent).Name, evento.EventId, assinatura.HandlerType.Name);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao entregar {Evento} ({EventId}) para {Handler}")]
    private partial void LogFalhaNoAssinante(Exception ex, string evento, Guid eventId, string handler);
}

public static class EventBusExtensions
{
    /// <summary>Registra o barramento em processo (uma vez, no Host).</summary>
    public static IServiceCollection AddInProcessEventBus(this IServiceCollection services)
    {
        services.TryAddSingleton<IEventBus, InProcessEventBus>();
        return services;
    }

    /// <summary>
    /// Assina um evento de integração: chamado no <c>Register</c> do módulo que REAGE ao evento.
    /// O handler é resolvido num escopo novo a cada entrega.
    /// </summary>
    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddScoped<THandler>();
        services.AddSingleton(new IntegrationEventSubscription(typeof(TEvent), typeof(THandler)));
        return services;
    }
}
