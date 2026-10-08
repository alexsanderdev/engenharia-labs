using System.Text;
using RabbitMQ.Client;

namespace F6M05.Sagas.Mensageria;

/// <summary>
/// PRONTO. O que o manipulador de mensagens enxerga de uma entrega do RabbitMQ: uma cópia
/// imutável (o <c>Body</c> do RabbitMQ.Client 7 só é válido durante o callback).
/// </summary>
/// <param name="Fila">Fila de onde a mensagem foi consumida.</param>
/// <param name="MessageId">Identidade da mensagem (base da idempotência). Vazio se o produtor não informou.</param>
/// <param name="Tipo">Tipo da mensagem (propriedade AMQP <c>type</c>).</param>
/// <param name="CorrelationId">Correlação (no lab, o id do pedido).</param>
/// <param name="ContentType">Content type (no lab, <c>application/json</c>).</param>
/// <param name="Corpo">Corpo bruto.</param>
/// <param name="Cabecalhos">Headers AMQP (valores textuais chegam como <c>byte[]</c>; use <see cref="Mensageria.Cabecalhos"/>).</param>
/// <param name="Reentregue">Flag <c>redelivered</c> do broker (a mensagem já foi entregue antes sem ack).</param>
public sealed record MensagemRecebida(
    string Fila,
    string MessageId,
    string? Tipo,
    string? CorrelationId,
    string? ContentType,
    ReadOnlyMemory<byte> Corpo,
    IReadOnlyDictionary<string, object?> Cabecalhos,
    bool Reentregue)
{
    /// <summary>Retentativas atrasadas já feitas (header <see cref="Mensageria.Cabecalhos.Tentativas"/>).</summary>
    public int TentativasAtrasadas => Mensageria.Cabecalhos.LerInteiro(Cabecalhos, Mensageria.Cabecalhos.Tentativas);

    /// <summary>Quantas vezes a mensagem já voltou da DLQ.</summary>
    public int Reprocessamentos => Mensageria.Cabecalhos.LerInteiro(Cabecalhos, Mensageria.Cabecalhos.Reprocessamentos);

    /// <summary>Número da tentativa imediata em curso (0 = primeira execução nesta entrega).</summary>
    public int TentativaImediata { get; init; }

    /// <summary>Corpo como texto UTF-8.</summary>
    public string CorpoComoTexto => Encoding.UTF8.GetString(Corpo.Span);

    /// <summary>Desserializa o corpo. JSON inválido lança <see cref="System.Text.Json.JsonException"/> (erro permanente).</summary>
    public T Ler<T>() => Serializador.Desserializar<T>(Corpo);

    /// <summary>Copia uma entrega do RabbitMQ.Client (corpo e headers) para um registro imutável.</summary>
    public static MensagemRecebida De(string fila, IReadOnlyBasicProperties propriedades, ReadOnlyMemory<byte> corpo, bool reentregue) =>
        new(
            fila,
            propriedades.MessageId ?? "",
            propriedades.Type,
            propriedades.CorrelationId,
            propriedades.ContentType,
            corpo.ToArray(),
            Mensageria.Cabecalhos.Copiar(propriedades.Headers),
            reentregue);

    /// <summary>
    /// Propriedades AMQP para republicar esta mensagem (fila de espera, DLQ, reprocessamento),
    /// preservando MessageId, tipo, correlação e headers, e aplicando as alterações pedidas.
    /// </summary>
    /// <param name="alterar">Ajusta os headers da cópia (adicionar/remover).</param>
    public BasicProperties PropriedadesParaRepublicar(Action<Dictionary<string, object?>>? alterar = null)
    {
        var cabecalhos = new Dictionary<string, object?>(Cabecalhos);
        alterar?.Invoke(cabecalhos);
        return new BasicProperties
        {
            MessageId = MessageId,
            Type = Tipo,
            CorrelationId = CorrelationId,
            ContentType = ContentType,
            DeliveryMode = DeliveryModes.Persistent,
            Headers = cabecalhos,
        };
    }
}
