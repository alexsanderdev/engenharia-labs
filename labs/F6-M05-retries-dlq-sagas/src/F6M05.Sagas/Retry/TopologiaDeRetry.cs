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

        // TODO (Passo 2), com os nomes de PoliticaDeRetry (NomeDaDlq, NomeDoExchangeDeReentrada, NomeDaFilaDeEspera):
        //  1. DLQ: QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false).
        //  2. Fila principal durável com "x-dead-letter-exchange" = "" e "x-dead-letter-routing-key" = dlq
        //     (rede de segurança para nack sem requeue).
        //  3. Exchange direct de reentrada + QueueBindAsync(fila, reentrada, routingKey: fila).
        //  4. Para cada nível i (1..Atrasos.Count): fila de espera com
        //     "x-message-ttl" = (int)Atrasos[i-1].TotalMilliseconds,
        //     "x-dead-letter-exchange" = reentrada, "x-dead-letter-routing-key" = fila.
        await Task.CompletedTask;
        throw new NotImplementedException("TODO: declare DLQ, fila principal, exchange de reentrada e filas de espera com TTL (Passo 2).");
    }
}
