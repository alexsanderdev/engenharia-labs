using F6M01.Mensageria.Contratos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace F6M01.Mensageria.Consumo;

/// <summary>O que o consumidor decidiu fazer com a mensagem. (PRONTO)</summary>
public enum Decisao
{
    /// <summary>Processada: <c>BasicAck</c>. O broker apaga a mensagem.</summary>
    Confirmar,

    /// <summary>Falha transitória: <c>BasicNack(requeue: true)</c>. A mensagem volta para a fila.</summary>
    Reprocessar,

    /// <summary>Falha permanente: <c>BasicNack(requeue: false)</c>. Sem DLQ configurada, a mensagem é descartada
    /// (a DLQ entra no módulo 6.05).</summary>
    Descartar,
}

/// <summary>Mensagem entregue ao handler. (PRONTO)</summary>
/// <param name="Envelope">Envelope reconstruído das propriedades AMQP.</param>
/// <param name="Reentregue">O broker já entregou esta mensagem antes (flag <c>redelivered</c>).</param>
/// <param name="RoutingKey">Routing key com que a mensagem foi publicada.</param>
public sealed record MensagemRecebida(Envelope Envelope, bool Reentregue, string RoutingKey);

/// <summary>
/// Consumidor de UMA fila com ack manual e prefetch. Várias instâncias na mesma fila = competing consumers.
/// </summary>
public sealed class ConsumidorDeFila : IAsyncDisposable
{
    private readonly IChannel _canal;
    private readonly Func<MensagemRecebida, CancellationToken, Task<Decisao>> _processar;

    private ConsumidorDeFila(IChannel canal, Func<MensagemRecebida, CancellationToken, Task<Decisao>> processar)
    {
        _canal = canal;
        _processar = processar;
    }

    /// <summary>
    /// Abre um canal na <paramref name="conexao"/>, limita as mensagens não confirmadas com
    /// <c>BasicQosAsync(0, prefetch, global: false)</c>, registra um <see cref="AsyncEventingBasicConsumer"/>
    /// (evento <c>ReceivedAsync</c> → <see cref="AoReceberAsync"/>) e começa a consumir com <c>autoAck: false</c>.
    /// </summary>
    public static async Task<ConsumidorDeFila> IniciarAsync(
        IConnection conexao,
        string fila,
        ushort prefetch,
        Func<MensagemRecebida, CancellationToken, Task<Decisao>> processar,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conexao);
        ArgumentException.ThrowIfNullOrWhiteSpace(fila);
        ArgumentNullException.ThrowIfNull(processar);
        ArgumentOutOfRangeException.ThrowIfZero(prefetch);

        var canal = await conexao.CreateChannelAsync(cancellationToken: ct);
        await canal.BasicQosAsync(prefetchSize: 0, prefetchCount: prefetch, global: false, cancellationToken: ct);

        var consumidor = new ConsumidorDeFila(canal, processar);
        var basico = new AsyncEventingBasicConsumer(canal);
        basico.ReceivedAsync += consumidor.AoReceberAsync;
        await canal.BasicConsumeAsync(fila, autoAck: false, consumer: basico, cancellationToken: ct);

        return consumidor;
    }

    /// <summary>
    /// Para cada entrega:
    /// <list type="number">
    /// <item>reconstrói o envelope com <see cref="MapeamentoAmqp.ParaEnvelope"/>; se lançar
    /// <see cref="ContratoIncompativelException"/>, a mensagem é venenosa: <see cref="Decisao.Descartar"/>;</item>
    /// <item>chama o handler; se ele LANÇAR, a decisão é <see cref="Decisao.Reprocessar"/> na primeira entrega e
    /// <see cref="Decisao.Descartar"/> se a mensagem já era reentregue (<c>ea.Redelivered</c>): uma segunda chance,
    /// sem loop infinito;</item>
    /// <item>aplica a decisão: Confirmar → <c>BasicAckAsync(tag, multiple: false)</c>; Reprocessar →
    /// <c>BasicNackAsync(tag, false, requeue: true)</c>; Descartar → <c>BasicNackAsync(tag, false, requeue: false)</c>.</item>
    /// </list>
    /// Se o canal já estiver fechado na hora do ack (consumidor caiu), não há o que fazer: o broker
    /// devolverá a mensagem para a fila. Ignore <see cref="AlreadyClosedException"/> nesse caso.
    /// </summary>
    private async Task AoReceberAsync(object sender, BasicDeliverEventArgs ea)
    {
        Decisao decisao;
        try
        {
            var envelope = MapeamentoAmqp.ParaEnvelope(ea.BasicProperties, ea.Body);
            try
            {
                decisao = await _processar(new MensagemRecebida(envelope, ea.Redelivered, ea.RoutingKey), ea.CancellationToken);
            }
#pragma warning disable CA1031 // o consumidor precisa decidir algo para QUALQUER falha do handler
            catch (Exception)
#pragma warning restore CA1031
            {
                decisao = ea.Redelivered ? Decisao.Descartar : Decisao.Reprocessar;
            }
        }
        catch (ContratoIncompativelException)
        {
            decisao = Decisao.Descartar;
        }

        try
        {
            switch (decisao)
            {
                case Decisao.Confirmar:
                    await _canal.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    break;
                case Decisao.Reprocessar:
                    await _canal.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    break;
                default:
                    await _canal.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    break;
            }
        }
        catch (AlreadyClosedException)
        {
            // O canal caiu: a mensagem não confirmada volta para a fila sozinha (at-least-once).
        }
    }

    /// <summary>Fecha o canal: as mensagens entregues e ainda não confirmadas voltam para a fila. (PRONTO)</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_canal.IsOpen)
                await _canal.CloseAsync();
        }
        catch (AlreadyClosedException)
        {
        }

        await _canal.DisposeAsync();
    }
}
