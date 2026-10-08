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
        throw new NotImplementedException("TODO (Passo 3): _sender.SendMessageAsync(MensagensDePedido.CriarPedidoCriado(...), ct).");

    /// <summary>
    /// Publica vários pedidos usando <see cref="ServiceBusMessageBatch"/> (uma ida ao broker por lote,
    /// respeitando o tamanho máximo). Se uma mensagem não couber no lote atual, envie o lote e comece outro.
    /// Devolve quantos lotes foram enviados.
    /// </summary>
    public Task<int> PublicarLoteAsync(IReadOnlyList<PedidoCriado> eventos, string correlationId, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 3): crie o lote com await _sender.CreateMessageBatchAsync(ct) e use lote.TryAddMessage(...) para cada pedido; " +
            "se não couber, envie o lote (SendMessagesAsync) e crie outro. Devolva quantos lotes foram enviados.");

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
