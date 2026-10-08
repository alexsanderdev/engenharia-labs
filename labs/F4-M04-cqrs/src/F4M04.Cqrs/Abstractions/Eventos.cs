namespace F4M04.Cqrs.Abstractions;

/// <summary>Fato que aconteceu no domínio (verbo no passado: PedidoCriado, PedidoConfirmado).</summary>
public interface IDomainEvent
{
    DateTimeOffset OcorreuEm { get; }
}

#pragma warning disable CA1711 // "EventHandler" aqui é o nome consagrado do padrão, não um delegate de evento .NET
/// <summary>Reage a um evento de domínio dentro do mesmo processo (ex.: atualizar uma projeção).</summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct);
}
#pragma warning restore CA1711

/// <summary>Entrega eventos de domínio a todos os handlers registrados para o tipo do evento.</summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct);
}
