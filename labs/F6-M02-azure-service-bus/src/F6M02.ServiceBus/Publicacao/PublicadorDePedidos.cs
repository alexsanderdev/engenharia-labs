using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Contratos;
using F6M02.ServiceBus.Topologia;

namespace F6M02.ServiceBus.Publicacao;

/// <summary>
/// Publica <see cref="PedidoCriado"/> no tópico <see cref="Entidades.TopicoPedidos"/>.
/// O <see cref="ServiceBusSender"/> é caro de criar (abre um link AMQP) e é thread-safe:
/// crie UM por entidade e reaproveite (aqui, um por instância do publicador, que deve ser singleton).
/// </summary>
public sealed class PublicadorDePedidos : IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    /// <summary>Cria o sender do tópico de pedidos a partir do <paramref name="cliente"/> (singleton da aplicação).</summary>
    public PublicadorDePedidos(ServiceBusClient cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        _sender = cliente.CreateSender(Entidades.TopicoPedidos);
    }

    /// <summary>Publica um pedido (uma mensagem montada por <see cref="MensagensDePedido.CriarPedidoCriado"/>).</summary>
    public Task PublicarAsync(PedidoCriado evento, string correlationId, CancellationToken ct = default) =>
        _sender.SendMessageAsync(MensagensDePedido.CriarPedidoCriado(evento, correlationId), ct);

    /// <summary>
    /// Publica vários pedidos usando <see cref="ServiceBusMessageBatch"/> (uma ida ao broker por lote,
    /// respeitando o tamanho máximo). Se uma mensagem não couber no lote atual, envie o lote e comece outro.
    /// Devolve quantos lotes foram enviados.
    /// </summary>
    public async Task<int> PublicarLoteAsync(IReadOnlyList<PedidoCriado> eventos, string correlationId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(eventos);
        if (eventos.Count == 0) return 0;

        var lotes = 0;
        var lote = await _sender.CreateMessageBatchAsync(ct);
        try
        {
            foreach (var evento in eventos)
            {
                var mensagem = MensagensDePedido.CriarPedidoCriado(evento, correlationId);
                if (lote.TryAddMessage(mensagem)) continue;

                // Lote cheio: envia o que tem e começa outro.
                if (lote.Count == 0)
                    throw new InvalidOperationException($"O pedido {evento.PedidoId} não cabe sozinho num lote.");

                await _sender.SendMessagesAsync(lote, ct);
                lotes++;
                lote.Dispose();
                lote = await _sender.CreateMessageBatchAsync(ct);
                if (!lote.TryAddMessage(mensagem))
                    throw new InvalidOperationException($"O pedido {evento.PedidoId} não cabe sozinho num lote.");
            }

            await _sender.SendMessagesAsync(lote, ct);
            return lotes + 1;
        }
        finally
        {
            lote.Dispose();
        }
    }

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
