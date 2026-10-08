namespace F4M06.Shared.Eventos;

/// <summary>
/// Evento de integração: fato que um módulo PUBLICA para outros módulos.
/// Mora no projeto <c>.Contracts</c> do módulo que publica (faz parte do contrato público dele),
/// é imutável e carrega os dados de que o assinante precisa (sem obrigá-lo a consultar o publicador).
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Identidade do evento (útil para deduplicar no assinante).</summary>
    Guid EventId { get; }

    /// <summary>Quando o fato aconteceu.</summary>
    DateTimeOffset OcorridoEm { get; }
}

/// <summary>Assinante de um evento de integração. Mora no módulo que REAGE ao evento e é <c>internal</c>.</summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent evento, CancellationToken ct = default);
}

/// <summary>Publica eventos de integração para quem assinou (o publicador não sabe quem é).</summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent evento, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
