using System.Text.Json;

namespace MP3.Contratos;

/// <summary>
/// Combinados entre quem publica (API do OrderFlow) e quem consome (este worker).
/// Sem dependência do SDK do Service Bus: o contrato não deve obrigar ninguém a usar uma biblioteca.
/// </summary>
public static class ConvencoesDeMensagem
{
    /// <summary>Fila de entrada do worker. No OrderFlow, a subscription "notificacoes" do tópico "pedidos" faz ForwardTo para ela.</summary>
    public const string FilaDeNotificacoes = "notificacoes-pedidos";

    /// <summary>Valor de <c>Subject</c> (label) das mensagens deste contrato.</summary>
    public const string Assunto = nameof(PedidoCriado);

    public const string ContentType = "application/json";

    /// <summary>Propriedade de aplicação com o contexto W3C do trace da API (<c>00-traceId-spanId-flags</c>).</summary>
    public const string PropriedadeTraceparent = "traceparent";

    /// <summary>JSON em camelCase, igual ao da API.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>UTF-8 JSON, pronto para virar o corpo da mensagem.</summary>
    public static byte[] Serializar(PedidoCriado evento) => JsonSerializer.SerializeToUtf8Bytes(evento, Json);

    /// <summary>Lança <see cref="JsonException"/> se o corpo não for um <see cref="PedidoCriado"/> válido.</summary>
    public static PedidoCriado Desserializar(ReadOnlySpan<byte> utf8) =>
        JsonSerializer.Deserialize<PedidoCriado>(utf8, Json) ?? throw new JsonException("Corpo vazio.");
}
