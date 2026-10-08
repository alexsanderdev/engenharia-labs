using F4M06.Pedidos.Contracts;
using F4M06.Shared.Eventos;

namespace F4M06.Clientes.Integracao;

/// <summary>
/// Clientes REAGE a <see cref="PedidoConfirmado"/>: soma o total gasto e recalcula a fidelidade.
/// Precisa ser idempotente: o mesmo pedido entregue duas vezes é contabilizado uma vez só.
/// </summary>
internal sealed class PedidoConfirmadoHandler : IIntegrationEventHandler<PedidoConfirmado>
{
    public Task HandleAsync(PedidoConfirmado evento, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 5): receba ClientesDbContext e TimeProvider pelo construtor. Se o PedidoId já está em " +
            "PedidosContabilizados, ignore (idempotência). Senão, carregue o cliente, chame RegistrarCompra(evento.Total), " +
            "adicione um PedidoContabilizado e grave tudo num ÚNICO SaveChangesAsync. " +
            "Depois assine o evento no ClientesModule.Register com AddIntegrationEventHandler<PedidoConfirmado, PedidoConfirmadoHandler>().");
}
