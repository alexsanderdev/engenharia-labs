using F4M06.Shared.Eventos;

namespace F4M06.Pedidos.Contracts;

/// <summary>
/// Evento de integração publicado por Pedidos quando um pedido é confirmado.
/// Faz parte do contrato público do módulo: mudar este record é mudar uma API (versione com cuidado).
/// Carrega o que os assinantes precisam (cliente e total), para que ninguém precise consultar Pedidos de volta.
/// </summary>
public sealed record PedidoConfirmado(
    Guid EventId,
    DateTimeOffset OcorridoEm,
    Guid PedidoId,
    Guid ClienteId,
    decimal Total) : IIntegrationEvent;
