using RabbitMQ.Client;

namespace F6M05.Sagas.Mensageria;

/// <summary>
/// PRONTO. Um canal AMQP só para PUBLICAR, com <b>publisher confirms</b> ligados: cada
/// <see cref="PublicarAsync"/> só termina depois que o broker confirmou que a mensagem foi
/// gravada na fila (ou lança). Com <c>mandatory: true</c>, publicar para uma rota sem fila
/// também vira exceção (<c>PublishReturnException</c>) em vez de sumir em silêncio.
/// </summary>
/// <remarks>
/// Por que um canal separado do canal de consumo? O ack de uma entrega precisa sair no canal
/// que a recebeu, mas misturar publicação com confirmação e consumo no mesmo canal complica a
/// ordem dos frames e o controle de fluxo. Canais não são thread-safe para publicação
/// concorrente; o semáforo serializa as publicações.
/// </remarks>
public sealed class CanalDePublicacao : IAsyncDisposable
{
    private readonly SemaphoreSlim _trava = new(1, 1);

    private CanalDePublicacao(IChannel canal) => Canal = canal;

    /// <summary>O canal subjacente (para declarar topologia, se necessário).</summary>
    public IChannel Canal { get; }

    public static async Task<CanalDePublicacao> CriarAsync(IConnection conexao, CancellationToken ct = default)
    {
        var canal = await conexao.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);
        return new CanalDePublicacao(canal);
    }

    /// <summary>Publica e espera a confirmação do broker.</summary>
    public async Task PublicarAsync(
        string exchange, string routingKey, BasicProperties propriedades, ReadOnlyMemory<byte> corpo, CancellationToken ct = default)
    {
        await _trava.WaitAsync(ct);
        try
        {
            await Canal.BasicPublishAsync(exchange, routingKey, mandatory: true, propriedades, corpo, ct);
        }
        finally
        {
            _trava.Release();
        }
    }

    /// <summary>Serializa <paramref name="mensagem"/> em JSON e publica com MessageId, tipo e correlação.</summary>
    public Task PublicarJsonAsync<T>(
        string exchange, string routingKey, T mensagem, string messageId, string? tipo = null,
        string? correlationId = null, CancellationToken ct = default)
    {
        var propriedades = new BasicProperties
        {
            MessageId = messageId,
            Type = tipo ?? typeof(T).Name,
            CorrelationId = correlationId,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>(),
        };
        return PublicarAsync(exchange, routingKey, propriedades, Serializador.Serializar(mensagem), ct);
    }

    public async ValueTask DisposeAsync()
    {
        await Canal.DisposeAsync();
        _trava.Dispose();
    }
}
