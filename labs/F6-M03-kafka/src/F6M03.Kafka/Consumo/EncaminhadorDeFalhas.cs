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
        await Task.CompletedTask;
        throw new NotImplementedException(
            "TODO (Passo 7): calcule tentativas (header x-tentativas + 1), escolha AdministradorDeTopicos.NomeRetry/NomeDlq, " +
            "copie os headers que não começam com \"x-\", acrescente x-tentativas, x-erro-tipo, x-erro-mensagem, " +
            "x-topico-original, x-particao-original e x-offset-original e publique com _producer.ProduceAsync(destino, ...).");
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
