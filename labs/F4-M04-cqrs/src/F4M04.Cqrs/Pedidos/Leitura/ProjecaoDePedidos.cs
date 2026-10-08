using F4M04.Cqrs.Abstractions;
using F4M04.Cqrs.Pedidos.Dominio;

namespace F4M04.Cqrs.Pedidos.Leitura;

/// <summary>
/// Projeção: transforma eventos de domínio em linhas do read model.
/// É a ÚNICA classe que escreve no <see cref="BancoDeLeitura"/>.
/// </summary>
public sealed class ProjecaoDePedidos(BancoDeLeitura leitura)
    : IDomainEventHandler<PedidoCriado>, IDomainEventHandler<PedidoConfirmado>
{
    /// <summary>Cria (ou substitui, para ser idempotente) o resumo com status "Created".</summary>
    public Task HandleAsync(PedidoCriado domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        leitura.Pedidos[domainEvent.PedidoId] = new PedidoResumo(
            domainEvent.PedidoId,
            domainEvent.ClienteId,
            nameof(StatusPedido.Created),
            domainEvent.Total,
            domainEvent.QuantidadeItens,
            domainEvent.OcorreuEm,
            ConfirmadoEm: null);
        return Task.CompletedTask;
    }

    /// <summary>Atualiza status e data de confirmação, preservando os demais campos. Pedido desconhecido é ignorado.</summary>
    public Task HandleAsync(PedidoConfirmado domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (leitura.Pedidos.TryGetValue(domainEvent.PedidoId, out var atual))
        {
            leitura.Pedidos[domainEvent.PedidoId] = atual with
            {
                Status = nameof(StatusPedido.Confirmed),
                ConfirmadoEm = domainEvent.OcorreuEm,
            };
        }
        return Task.CompletedTask;
    }
}
