using F2M08.Application.Abstracoes;
using F2M08.Domain.Repositorios;

namespace F2M08.Application.Pedidos;

public sealed record CancelarPedido(Guid PedidoId);

/// <summary>Caso de uso "cancelar pedido". Devolve false se o pedido não existe.</summary>
public sealed class CancelarPedidoHandler(IPedidoRepository pedidos) : ICommandHandler<CancelarPedido, bool>
{
    public async Task<bool> HandleAsync(CancelarPedido command, CancellationToken ct)
    {
        var pedido = await pedidos.ObterAsync(command.PedidoId, ct);
        if (pedido is null) return false;

        pedido.Cancelar();
        await pedidos.SalvarAsync(ct);
        return true;
    }
}
