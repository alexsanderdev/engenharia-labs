using F6M04.Pedidos.Dominio;
using F6M04.Pedidos.Infra;
using F6M04.Pedidos.Mensageria;
using F6M04.Pedidos.Outbox;
using Microsoft.EntityFrameworkCore;

namespace F6M04.Pedidos.Aplicacao;

/// <summary>Comando de criação de pedido.</summary>
public sealed record CriarPedido(string Numero, string ClienteEmail, decimal Total);

/// <summary>
/// Casos de uso de Pedidos.
/// </summary>
/// <remarks>
/// TODO (Passo 1): esta é a versão "DUAL WRITE" — salva no banco e DEPOIS publica direto no broker. Se o broker
/// estiver fora do ar (ou o processo cair entre as duas linhas), o pedido existe e o evento se perde para sempre
/// (veja <c>Demonstracoes/DualWriteTests</c>). Corrija assim:
/// <list type="number">
/// <item>remova o <see cref="IPublicadorDeMensagens"/> do construtor e todo o código de publicação;</item>
/// <item>NÃO limpe os eventos do agregado: deixe o <see cref="OutboxInterceptor"/> gravá-los na Outbox durante o
/// <c>SaveChanges</c> (mesma transação do pedido);</item>
/// <item>quem publica é o <see cref="OutboxProcessor"/>, depois, em segundo plano.</item>
/// </list>
/// </remarks>
public sealed class ServicoDePedidos(PedidosDbContext db, IPublicadorDeMensagens publicador, TimeProvider relogio)
{
    public async Task<Guid> CriarAsync(CriarPedido comando, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var pedido = Pedido.Criar(comando.Numero, comando.ClienteEmail, comando.Total, relogio.GetUtcNow());
        var mensagens = OutboxMessage.DoAgregado(pedido).Select(m => m.ParaMensagemDeSaida()).ToList();
        pedido.LimparEventos();

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);                                         // 1ª escrita: banco
        foreach (var m in mensagens) await publicador.PublicarAsync(m, ct);    // 2ª escrita: broker (dual write!)
        return pedido.Id;
    }

    public async Task ConfirmarAsync(Guid pedidoId, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos.SingleOrDefaultAsync(p => p.Id == pedidoId, ct)
            ?? throw new KeyNotFoundException($"Pedido {pedidoId} não encontrado.");

        pedido.Confirmar(relogio.GetUtcNow());
        var mensagens = OutboxMessage.DoAgregado(pedido).Select(m => m.ParaMensagemDeSaida()).ToList();
        pedido.LimparEventos();

        await db.SaveChangesAsync(ct);                                         // 1ª escrita: banco
        foreach (var m in mensagens) await publicador.PublicarAsync(m, ct);    // 2ª escrita: broker (dual write!)
    }
}
