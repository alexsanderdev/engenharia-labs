using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Contratos;
using F6M02.ServiceBus.Publicacao;
using F6M02.ServiceBus.Topologia;

namespace F6M02.ServiceBus.Pagamentos;

/// <summary>
/// Agenda lembretes de pagamento na fila <see cref="Entidades.FilaLembretes"/>: a mensagem é aceita pelo broker
/// agora, mas só fica visível para os consumidores no horário pedido.
/// </summary>
public sealed class AgendadorDeLembretes(ServiceBusClient cliente) : IAsyncDisposable
{
    private readonly ServiceBusSender _sender = cliente.CreateSender(Entidades.FilaLembretes);

    /// <summary>
    /// Agenda o lembrete para <paramref name="quando"/> com <c>ScheduleMessageAsync</c>
    /// (corpo JSON, <c>MessageId = "lembrete-{PedidoId:N}"</c>, <c>Subject = "LembreteDePagamento"</c>).
    /// Devolve o <c>SequenceNumber</c> do agendamento (necessário para cancelar).
    /// </summary>
    public Task<long> AgendarAsync(LembreteDePagamento lembrete, DateTimeOffset quando, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(lembrete);
        var mensagem = new ServiceBusMessage(BinaryData.FromObjectAsJson(lembrete, MensagensDePedido.Json))
        {
            MessageId = $"lembrete-{lembrete.PedidoId:N}",
            Subject = "LembreteDePagamento",
            ContentType = MensagensDePedido.ContentTypeJson,
        };
        return _sender.ScheduleMessageAsync(mensagem, quando, ct);
    }

    /// <summary>
    /// Cancela um agendamento (o cliente pagou antes do lembrete).
    /// Atenção: no emulador (v1.0.0) o cancelamento some do peek, mas a mensagem ainda é entregue no horário;
    /// por isso não há teste para ele no lab. No Azure real funciona.
    /// </summary>
    public Task CancelarAsync(long sequenceNumber, CancellationToken ct = default) =>
        _sender.CancelScheduledMessageAsync(sequenceNumber, ct);

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
