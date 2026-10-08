using F4M06.Clientes.Dominio;
using F4M06.Clientes.Infra;
using F4M06.Pedidos.Contracts;
using F4M06.Shared.Eventos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace F4M06.Clientes.Integracao;

/// <summary>
/// Clientes REAGE a <see cref="PedidoConfirmado"/>: soma o total gasto e recalcula a fidelidade.
/// Idempotente: o mesmo pedido entregue duas vezes é contabilizado uma vez só
/// (a marca <see cref="PedidoContabilizado"/> e a atualização do cliente vão no MESMO SaveChanges).
/// </summary>
internal sealed partial class PedidoConfirmadoHandler(
    ClientesDbContext db,
    TimeProvider relogio,
    ILogger<PedidoConfirmadoHandler> logger) : IIntegrationEventHandler<PedidoConfirmado>
{
    public async Task HandleAsync(PedidoConfirmado evento, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evento);

        if (await db.PedidosContabilizados.AnyAsync(p => p.PedidoId == evento.PedidoId, ct))
        {
            LogDuplicado(evento.PedidoId);
            return;
        }

        if (await db.Clientes.SingleOrDefaultAsync(c => c.Id == evento.ClienteId, ct) is not { } cliente)
        {
            LogClienteNaoEncontrado(evento.ClienteId, evento.PedidoId);
            return;
        }

        cliente.RegistrarCompra(evento.Total);
        db.PedidosContabilizados.Add(new PedidoContabilizado(evento.PedidoId, evento.ClienteId, evento.Total, relogio.GetUtcNow()));
        await db.SaveChangesAsync(ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pedido {PedidoId} já contabilizado; evento duplicado ignorado")]
    private partial void LogDuplicado(Guid pedidoId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cliente {ClienteId} do pedido {PedidoId} não encontrado")]
    private partial void LogClienteNaoEncontrado(Guid clienteId, Guid pedidoId);
}
