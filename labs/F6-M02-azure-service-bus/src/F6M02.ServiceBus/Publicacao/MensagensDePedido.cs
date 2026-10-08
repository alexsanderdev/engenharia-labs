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
    public static ServiceBusMessage CriarPedidoCriado(PedidoCriado evento, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(evento);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var mensagem = new ServiceBusMessage(BinaryData.FromObjectAsJson(evento, Json))
        {
            MessageId = $"pedido-criado-{evento.PedidoId:N}",
            CorrelationId = correlationId,
            Subject = SubjectPedidoCriado,
            ContentType = ContentTypeJson,
        };

        // Application properties são o que os filtros enxergam: o broker NÃO abre o corpo.
        // Tipos simples (string, double, int, bool...). valorTotal vai como double para o SQL filter comparar números.
        mensagem.ApplicationProperties[Propriedades.Tipo] = SubjectPedidoCriado;
        mensagem.ApplicationProperties[Propriedades.ValorTotal] = (double)evento.ValorTotal;
        mensagem.ApplicationProperties[Propriedades.Segmento] = evento.Segmento;
        mensagem.ApplicationProperties[Propriedades.Canal] = evento.Canal;
        mensagem.ApplicationProperties[Propriedades.ClienteId] = evento.ClienteId.ToString();
        mensagem.ApplicationProperties[Propriedades.VersaoDoContrato] = 1;
        return mensagem;
    }

    /// <summary>
    /// Lê o <see cref="PedidoCriado"/> de uma mensagem recebida.
    /// Lança <see cref="MensagemInvalidaException"/> se o <c>ContentType</c> não for JSON, se o corpo não for
    /// um JSON válido do evento ou se o <c>PedidoId</c> vier vazio.
    /// </summary>
    public static PedidoCriado LerPedidoCriado(ServiceBusReceivedMessage mensagem)
    {
        ArgumentNullException.ThrowIfNull(mensagem);

        if (!string.Equals(mensagem.ContentType, ContentTypeJson, StringComparison.OrdinalIgnoreCase))
            throw new MensagemInvalidaException($"ContentType inesperado: '{mensagem.ContentType}'.");

        PedidoCriado? evento;
        try
        {
            evento = mensagem.Body.ToObjectFromJson<PedidoCriado>(Json);
        }
        catch (JsonException ex)
        {
            throw new MensagemInvalidaException($"Corpo não é um PedidoCriado válido: {ex.Message}", ex);
        }

        if (evento is null || evento.PedidoId == Guid.Empty)
            throw new MensagemInvalidaException("PedidoCriado sem PedidoId.");

        return evento;
    }
}
