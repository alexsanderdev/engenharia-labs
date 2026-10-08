using RabbitMQ.Client;

namespace F6M04.Notificacoes.Mensageria;

/// <summary>PRONTO. De onde o serviço de Notificações consome.</summary>
public sealed class NotificacoesRabbitMqOptions
{
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";

    /// <summary>Exchange (topic) onde Pedidos publica.</summary>
    public string Exchange { get; set; } = "orderflow.pedidos";

    /// <summary>Fila DESTE consumidor (cada consumidor tem a sua; a exchange faz o fan-out).</summary>
    public string Fila { get; set; } = "notificacoes.pedidos";

    /// <summary>Para onde vão as mensagens rejeitadas (dead-letter). Retry com espera e reprocessamento: módulo 6.05.</summary>
    public string FilaDeMensagensMortas { get; set; } = "notificacoes.pedidos.dlq";

    /// <summary>Padrão de routing key assinado (topic): todos os eventos de pedido.</summary>
    public string Assinatura { get; set; } = "pedido.*";

    /// <summary>Quantas mensagens não confirmadas o broker entrega de uma vez (back-pressure).</summary>
    public ushort Prefetch { get; set; } = 10;
}

/// <summary>
/// PRONTO. Topologia do lado do CONSUMIDOR: a exchange (idempotente, igual à do publicador), a fila durável
/// com dead-letter para a DLQ, a DLQ e o binding. Declarar duas vezes com os mesmos argumentos não faz nada.
/// </summary>
public static class TopologiaDeNotificacoes
{
    public static async Task DeclararAsync(IChannel canal, NotificacoesRabbitMqOptions o, CancellationToken ct = default)
    {
        await canal.ExchangeDeclareAsync(o.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);

        await canal.QueueDeclareAsync(o.FilaDeMensagensMortas, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

        await canal.QueueDeclareAsync(o.Fila, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",                    // exchange padrão...
                ["x-dead-letter-routing-key"] = o.FilaDeMensagensMortas, // ...roteia direto para a DLQ
            },
            cancellationToken: ct);

        await canal.QueueBindAsync(o.Fila, o.Exchange, o.Assinatura, cancellationToken: ct);
    }
}
