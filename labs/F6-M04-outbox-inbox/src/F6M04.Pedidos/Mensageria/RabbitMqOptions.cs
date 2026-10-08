using RabbitMQ.Client;

namespace F6M04.Pedidos.Mensageria;

/// <summary>PRONTO. Onde o contexto de Pedidos publica.</summary>
public sealed class RabbitMqOptions
{
    /// <summary>URI AMQP (ex.: <c>amqp://guest:guest@localhost:5672/</c>). Em produção, vem de segredo/identidade.</summary>
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";

    /// <summary>Exchange do tipo <c>topic</c> onde os eventos de Pedidos são publicados (routing key = tipo do evento).</summary>
    public string Exchange { get; set; } = "orderflow.pedidos";

    /// <summary>Tempo máximo para abrir a conexão (broker fora do ar deve falhar rápido, não pendurar o processor).</summary>
    public TimeSpan TempoMaximoDeConexao { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>PRONTO. Topologia do lado do PUBLICADOR: só a exchange (as filas são de quem consome).</summary>
public static class TopologiaDePedidos
{
    public static Task DeclararExchangeAsync(IChannel canal, string exchange, CancellationToken ct = default) =>
        canal.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
}
