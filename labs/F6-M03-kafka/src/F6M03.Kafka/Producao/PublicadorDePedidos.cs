using Confluent.Kafka;
using F6M03.Kafka.Contratos;

namespace F6M03.Kafka.Producao;

/// <summary>
/// Publica eventos de pedido num tópico. Um producer por processo (é thread-safe e caro de criar).
/// </summary>
public sealed class PublicadorDePedidos : IDisposable
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly string _topico;

    public PublicadorDePedidos(string bootstrapServers, string topico)
    {
        _topico = topico;
        _producer = new ProducerBuilder<string, byte[]>(CriarConfig(bootstrapServers)).Build();
    }

    /// <summary>
    /// Passo 3: configuração do producer "seguro para produção":
    /// <list type="bullet">
    /// <item><c>EnableIdempotence = true</c>: retries internos não duplicam nem reordenam mensagens na partição.</item>
    /// <item><c>Acks = Acks.All</c>: só confirma quando todas as réplicas em sincronia gravaram.</item>
    /// <item><c>Partitioner = Partitioner.Murmur2Random</c>: mesmo hash do client Java (um producer .NET e um
    /// Java mandam a mesma chave para a mesma partição; o padrão do librdkafka é outro, CRC32).</item>
    /// <item><c>LingerMs = 5</c> (lote pequeno, latência baixa no teste) e <c>ClientId</c> = "orderflow-pedidos".</item>
    /// </list>
    /// </summary>
    public static ProducerConfig CriarConfig(string bootstrapServers) =>
        throw new NotImplementedException(
            "TODO (Passo 3): devolva new ProducerConfig { BootstrapServers, ClientId = \"orderflow-pedidos\", " +
            "EnableIdempotence = true, Acks = Acks.All, Partitioner = Partitioner.Murmur2Random, LingerMs = 5 }.");

    /// <summary>
    /// Passo 3: publica o evento usando <see cref="SerializadorDeEventos.CriarMensagem"/> e espera a
    /// confirmação do broker (<see cref="IProducer{TKey,TValue}.ProduceAsync(string, Message{TKey,TValue}, CancellationToken)"/>).
    /// O <see cref="DeliveryResult{TKey,TValue}"/> diz em que partição e offset a mensagem ficou.
    /// </summary>
    public Task<DeliveryResult<string, byte[]>> PublicarAsync(IEventoDePedido evento, CancellationToken ct = default) =>
        throw new NotImplementedException(
            $"TODO (Passo 3): publique em '{_topico}' com _producer.ProduceAsync(_topico, SerializadorDeEventos.CriarMensagem(evento), ct).");

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
