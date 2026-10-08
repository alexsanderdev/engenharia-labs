using System.Text.Json;
using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Contratos;

namespace F6M02.ServiceBus.Publicacao;

/// <summary>
/// Tradução entre o evento de domínio <see cref="PedidoCriado"/> e a mensagem do Service Bus.
/// Concentrar isso num lugar só garante que publicador e consumidores falem o mesmo "contrato do fio".
/// </summary>
public static class MensagensDePedido
{
    /// <summary>Valor de <see cref="ServiceBusMessage.Subject"/> (o antigo <c>Label</c>) para o evento.</summary>
    public const string SubjectPedidoCriado = "PedidoCriado";

    /// <summary>Content type do corpo.</summary>
    public const string ContentTypeJson = "application/json";

    /// <summary>Nomes das application properties (usadas pelos filtros das subscriptions).</summary>
    public static class Propriedades
    {
        public const string Tipo = "tipo";
        public const string ValorTotal = "valorTotal";
        public const string Segmento = "segmento";
        public const string Canal = "canal";
        public const string ClienteId = "clienteId";
        public const string VersaoDoContrato = "versao";
    }

    /// <summary>Opções de JSON do contrato (camelCase, como numa API web).</summary>
    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Monta a mensagem de <see cref="PedidoCriado"/>:
    /// <list type="bullet">
    /// <item>corpo: o evento em JSON (<see cref="Json"/>), <c>ContentType = application/json</c>;</item>
    /// <item><c>MessageId = "pedido-criado-{PedidoId:N}"</c> — determinístico, para a detecção de duplicatas e a idempotência dos consumidores funcionarem;</item>
    /// <item><c>Subject = "PedidoCriado"</c> e <c>CorrelationId</c> = o correlation id recebido (rastreamento ponta a ponta);</item>
    /// <item>application properties: <c>tipo</c>, <c>valorTotal</c> (double), <c>segmento</c>, <c>canal</c>, <c>clienteId</c> (string) e <c>versao</c> = 1.</item>
    /// </list>
    /// </summary>
    public static ServiceBusMessage CriarPedidoCriado(PedidoCriado evento, string correlationId) =>
        throw new NotImplementedException(
            "TODO (Passo 1): new ServiceBusMessage(BinaryData.FromObjectAsJson(evento, Json)) com MessageId determinístico, " +
            "CorrelationId, Subject e ContentType; depois preencha mensagem.ApplicationProperties (valorTotal como double!).");

    /// <summary>
    /// Lê o <see cref="PedidoCriado"/> de uma mensagem recebida.
    /// Lança <see cref="MensagemInvalidaException"/> se o <c>ContentType</c> não for JSON, se o corpo não for
    /// um JSON válido do evento ou se o <c>PedidoId</c> vier vazio.
    /// </summary>
    public static PedidoCriado LerPedidoCriado(ServiceBusReceivedMessage mensagem) =>
        throw new NotImplementedException(
            "TODO (Passo 1): confira o ContentType, use mensagem.Body.ToObjectFromJson<PedidoCriado>(Json) e troque " +
            "JsonException/PedidoId vazio por MensagemInvalidaException.");
}
