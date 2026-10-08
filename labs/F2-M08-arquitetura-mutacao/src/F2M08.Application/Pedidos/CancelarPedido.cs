using F2M08.Application.Abstracoes;
using F2M08.Infrastructure.Persistencia;

namespace F2M08.Application.Pedidos;

public sealed record CancelarPedido(Guid PedidoId);

/// <summary>Caso de uso "cancelar pedido". Devolve false se o pedido não existe.</summary>
/// <remarks>
/// TODO (Passo 4): três problemas aqui. (1) O nome foge da convenção: implementações de
/// ICommandHandler terminam com "Handler". (2) A classe não é sealed. (3) Depende da classe
/// concreta da Infrastructure (resolva junto com o Passo 2, usando IPedidoRepository).
/// </remarks>
public class CancelarPedidoProcessor(PedidoRepositorioEmMemoria pedidos) : ICommandHandler<CancelarPedido, bool>
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
