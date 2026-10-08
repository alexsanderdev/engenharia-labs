using F6M04.Pedidos.Dominio;
using F6M04.Pedidos.Infra;
using Microsoft.EntityFrameworkCore;

namespace F6M04.Pedidos.Aplicacao;

/// <summary>Comando de criação de pedido.</summary>
public sealed record CriarPedido(string Numero, string ClienteEmail, decimal Total);

/// <summary>
/// Casos de uso de Pedidos. Repare no que NÃO tem aqui: nenhuma chamada ao broker. O caso de uso só muda o
/// agregado e salva; o <c>OutboxInterceptor</c> grava os eventos na mesma transação e o <c>OutboxProcessor</c>
/// publica depois. A requisição HTTP não depende de o broker estar no ar.
/// </summary>
public sealed class ServicoDePedidos(PedidosDbContext db, TimeProvider relogio)
{
    public async Task<Guid> CriarAsync(CriarPedido comando, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var pedido = Pedido.Criar(comando.Numero, comando.ClienteEmail, comando.Total, relogio.GetUtcNow());
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct); // pedido + OutboxMessage(PedidoCriado), atômicos
        return pedido.Id;
    }

    public async Task ConfirmarAsync(Guid pedidoId, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos.SingleOrDefaultAsync(p => p.Id == pedidoId, ct)
            ?? throw new KeyNotFoundException($"Pedido {pedidoId} não encontrado.");

        pedido.Confirmar(relogio.GetUtcNow());
        await db.SaveChangesAsync(ct); // status + OutboxMessage(PedidoConfirmado), atômicos
    }
}
