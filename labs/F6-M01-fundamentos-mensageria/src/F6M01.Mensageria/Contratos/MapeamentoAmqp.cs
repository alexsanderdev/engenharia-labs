using System.Globalization;
using System.Text;
using RabbitMQ.Client;

namespace F6M01.Mensageria.Contratos;

/// <summary>
/// Tradução entre o <see cref="Envelope"/> e as propriedades AMQP 0-9-1 do RabbitMQ.
/// </summary>
/// <remarks>
/// Propriedades padrão do AMQP usadas: <c>message_id</c>, <c>correlation_id</c>, <c>type</c>,
/// <c>content_type</c>, <c>delivery_mode</c> e <c>timestamp</c> (segundos: perde os milissegundos).
/// O que não tem propriedade padrão vai em headers: a versão do contrato e o instante exato de criação.
/// </remarks>
public static class MapeamentoAmqp
{
    /// <summary>Header com a versão do contrato (inteiro).</summary>
    public const string HeaderVersao = "versao-contrato";

    /// <summary>Header com o instante de criação em ISO 8601 ("O"), com milissegundos.</summary>
    public const string HeaderCriadoEm = "criado-em";

    /// <summary>Content type do corpo.</summary>
    public const string ContentTypeJson = "application/json";

    /// <summary>
    /// Monta as propriedades AMQP do envelope: <c>Persistent = true</c> (sobrevive a restart do broker,
    /// se a fila for durável), <c>ContentType</c> JSON, <c>MessageId</c> (Guid "D"), <c>CorrelationId</c>,
    /// <c>Type</c>, <c>Timestamp</c> (Unix em segundos) e os headers <see cref="HeaderVersao"/> (int) e
    /// <see cref="HeaderCriadoEm"/> (string "O").
    /// </summary>
    public static BasicProperties ParaPropriedades(Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        throw new NotImplementedException(
            "TODO (Passo 3): new BasicProperties { Persistent = true, ContentType, MessageId, CorrelationId, Type, " +
            "Timestamp = new AmqpTimestamp(segundos), Headers = { [HeaderVersao] = versão, [HeaderCriadoEm] = ToString(\"O\") } }");
    }

    /// <summary>
    /// Reconstrói o envelope a partir do que chegou do broker. O corpo é COPIADO (no RabbitMQ.Client 7
    /// a memória de <paramref name="corpo"/> só vale durante o callback do consumidor).
    /// Falta de MessageId/Type/versão, ou valores inválidos, viram <see cref="ContratoIncompativelException"/>
    /// (mensagem "venenosa": não adianta reprocessar).
    /// </summary>
    /// <remarks>
    /// Atenção: headers de texto voltam do broker como <c>byte[]</c> (UTF-8), não como <c>string</c>.
    /// Use <see cref="LerTexto"/> para aceitar os dois.
    /// </remarks>
    public static Envelope ParaEnvelope(IReadOnlyBasicProperties propriedades, ReadOnlyMemory<byte> corpo)
    {
        ArgumentNullException.ThrowIfNull(propriedades);

        throw new NotImplementedException(
            "TODO (Passo 3): valide MessageId (Guid) e Type, leia a versão (int) e o criado-em (LerTexto: string OU byte[]), " +
            "copie o corpo com ToArray(); dado faltando/ inválido → ContratoIncompativelException");
    }

    /// <summary>Lê um header de texto que pode vir como <c>string</c> ou <c>byte[]</c> UTF-8. (PRONTO)</summary>
    public static string? LerTexto(object? valor) => valor switch
    {
        string s => s,
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        _ => null,
    };
}
