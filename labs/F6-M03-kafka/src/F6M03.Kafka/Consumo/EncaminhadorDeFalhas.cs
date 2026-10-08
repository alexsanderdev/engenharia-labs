using System.Globalization;
using Confluent.Kafka;
using F6M03.Kafka.Contratos;
using F6M03.Kafka.Producao;
using F6M03.Kafka.Topicos;

namespace F6M03.Kafka.Consumo;

/// <summary>
/// Kafka não tem DLQ nativa (diferente do Service Bus/RabbitMQ): quem decide para onde vai a mensagem
/// que falhou é o consumidor. Este encaminhador republica a mensagem em "&lt;topico&gt;.retry" ou
/// "&lt;topico&gt;.dlq" com headers de diagnóstico, e o consumidor então faz commit do offset original.
/// </summary>
public sealed class EncaminhadorDeFalhas : IDisposable
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly string _topicoBase;
    private readonly int _maxTentativas;

    /// <param name="bootstrapServers">Endereço do cluster.</param>
    /// <param name="topicoBase">Tópico principal (ex.: "pedidos"); retry e DLQ derivam dele.</param>
    /// <param name="maxTentativas">Total de tentativas de processamento antes da DLQ (contando a primeira).</param>
    public EncaminhadorDeFalhas(string bootstrapServers, string topicoBase, int maxTentativas = 3)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTentativas, 1);
        _topicoBase = topicoBase;
        _maxTentativas = maxTentativas;
        _producer = new ProducerBuilder<string, byte[]>(PublicadorDePedidos.CriarConfig(bootstrapServers)).Build();
    }

    /// <summary>
    /// Passo 7: republica a mensagem que falhou e devolve o nome do tópico de destino.
    /// <list type="number">
    /// <item><c>tentativas</c> = valor do header <see cref="Cabecalhos.Tentativas"/> (ausente = 0) + 1.</item>
    /// <item>Destino = DLQ se o erro é <see cref="ContratoNaoSuportadoException"/> (não adianta repetir) ou se
    /// <c>tentativas &gt;= maxTentativas</c>; senão, o tópico de retry.</item>
    /// <item>Mesma chave e mesmo corpo. Copie os headers originais que NÃO começam com "x-" e acrescente:
    /// <see cref="Cabecalhos.Tentativas"/>, <see cref="Cabecalhos.ErroTipo"/> (FullName da exceção),
    /// <see cref="Cabecalhos.ErroMensagem"/>, <see cref="Cabecalhos.TopicoOriginal"/> (o que já vier no header, ou o
    /// tópico de onde a mensagem foi lida), <see cref="Cabecalhos.ParticaoOriginal"/> e <see cref="Cabecalhos.OffsetOriginal"/>.</item>
    /// </list>
    /// </summary>
    public async Task<string> EncaminharAsync(ConsumeResult<string, byte[]> falha, Exception erro, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(falha);
        ArgumentNullException.ThrowIfNull(erro);

        var anteriores = int.TryParse(Cabecalhos.Ler(falha.Message.Headers, Cabecalhos.Tentativas), out var n) ? n : 0;
        var tentativas = anteriores + 1;

        var destino = erro is ContratoNaoSuportadoException || tentativas >= _maxTentativas
            ? AdministradorDeTopicos.NomeDlq(_topicoBase)
            : AdministradorDeTopicos.NomeRetry(_topicoBase);

        var headers = new Headers();
        foreach (var h in falha.Message.Headers ?? [])
        {
            if (!h.Key.StartsWith("x-", StringComparison.Ordinal))
                headers.Add(h.Key, h.GetValueBytes());
        }

        var topicoOriginal = Cabecalhos.Ler(falha.Message.Headers, Cabecalhos.TopicoOriginal) ?? falha.Topic;
        Cabecalhos.Escrever(headers, Cabecalhos.Tentativas, tentativas.ToString(CultureInfo.InvariantCulture));
        Cabecalhos.Escrever(headers, Cabecalhos.ErroTipo, erro.GetType().FullName ?? erro.GetType().Name);
        Cabecalhos.Escrever(headers, Cabecalhos.ErroMensagem, erro.Message);
        Cabecalhos.Escrever(headers, Cabecalhos.TopicoOriginal, topicoOriginal);
        Cabecalhos.Escrever(headers, Cabecalhos.ParticaoOriginal, falha.Partition.Value.ToString(CultureInfo.InvariantCulture));
        Cabecalhos.Escrever(headers, Cabecalhos.OffsetOriginal, falha.Offset.Value.ToString(CultureInfo.InvariantCulture));

        await _producer.ProduceAsync(destino, new Message<string, byte[]>
        {
            Key = falha.Message.Key,
            Value = falha.Message.Value,
            Headers = headers,
        }, ct);

        return destino;
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
