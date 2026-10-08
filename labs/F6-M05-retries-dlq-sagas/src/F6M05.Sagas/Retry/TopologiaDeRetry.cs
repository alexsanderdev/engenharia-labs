using RabbitMQ.Client;

namespace F6M05.Sagas.Retry;

/// <summary>
/// Declara no RabbitMQ a topologia de retry atrasado e DLQ de uma fila.
/// </summary>
/// <remarks>
/// Para a fila <c>f</c> e uma política com N atrasos:
/// <code>
///   f                (principal, durável; DLX de segurança → f.dlq)
///   f.reentrada      (exchange direct; binding f.reentrada --[f]--> f)
///   f.espera.1..N    (x-message-ttl = Atrasos[i]; x-dead-letter-exchange = f.reentrada; x-dead-letter-routing-key = f)
///   f.dlq            (durável, sem consumidor: espera análise e reprocessamento)
/// </code>
/// Por que UMA fila de espera por nível, e não uma fila só com TTL por mensagem? O RabbitMQ só
/// expira mensagens na CABEÇA da fila: uma mensagem de 1 min na frente seguraria outra de 1 s
/// atrás dela (head-of-line blocking). Com TTL fixo por fila, todas na mesma fila expiram em ordem.
/// </remarks>
public static class TopologiaDeRetry
{
    /// <summary>
    /// Declara (idempotente) a fila principal, o exchange de reentrada, as filas de espera e a DLQ.
    /// </summary>
    public static async Task DeclararAsync(IChannel canal, string fila, PoliticaDeRetry politica, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(canal);
        ArgumentException.ThrowIfNullOrWhiteSpace(fila);
        ArgumentNullException.ThrowIfNull(politica);
        politica.Validar();

        var dlq = PoliticaDeRetry.NomeDaDlq(fila);
        var reentrada = PoliticaDeRetry.NomeDoExchangeDeReentrada(fila);

        // DLQ primeiro: a fila principal aponta para ela como rede de segurança.
        await canal.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);

        // Rede de segurança: um nack(requeue: false) fora do fluxo normal não some; vai para a DLQ
        // (sem o motivo detalhado, só o x-death do broker). O fluxo normal publica na DLQ com motivo.
        await canal.QueueDeclareAsync(fila, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = dlq,
            },
            cancellationToken: ct);

        await canal.ExchangeDeclareAsync(reentrada, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, cancellationToken: ct);
        await canal.QueueBindAsync(fila, reentrada, routingKey: fila, arguments: null, cancellationToken: ct);

        for (var nivel = 1; nivel <= politica.Atrasos.Count; nivel++)
        {
            var ttl = (int)politica.Atrasos[nivel - 1].TotalMilliseconds;
            await canal.QueueDeclareAsync(PoliticaDeRetry.NomeDaFilaDeEspera(fila, nivel),
                durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-message-ttl"] = ttl,
                    ["x-dead-letter-exchange"] = reentrada,
                    ["x-dead-letter-routing-key"] = fila,
                },
                cancellationToken: ct);
        }
    }
}
