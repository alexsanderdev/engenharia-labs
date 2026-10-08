namespace F6M02.ServiceBus.Consumo;

/// <summary>
/// O que o handler de negócio decidiu sobre a mensagem. O consumidor traduz isso em
/// Complete, Abandon ou DeadLetter — o handler não conhece o Service Bus.
/// </summary>
public abstract record ResultadoDoProcessamento
{
    private ResultadoDoProcessamento() { }

    /// <summary>Processou: a mensagem sai da fila (<c>CompleteMessageAsync</c>).</summary>
    public sealed record Sucesso : ResultadoDoProcessamento;

    /// <summary>
    /// Falhou por algo que pode passar (banco fora, timeout): devolve a mensagem para a fila
    /// (<c>AbandonMessageAsync</c>). Ela volta com <c>DeliveryCount + 1</c>; ao exceder o
    /// <c>MaxDeliveryCount</c>, o broker a move sozinho para a DLQ.
    /// </summary>
    public sealed record FalhaTransitoria(string Motivo) : ResultadoDoProcessamento;

    /// <summary>
    /// Nunca vai dar certo (regra de negócio, dado inválido): manda direto para a DLQ
    /// (<c>DeadLetterMessageAsync</c>) com motivo e descrição — sem gastar retentativas.
    /// </summary>
    public sealed record FalhaPermanente(string Motivo, string Descricao) : ResultadoDoProcessamento;

    public static readonly ResultadoDoProcessamento Ok = new Sucesso();
}

/// <summary>Metadados da mensagem que o handler pode precisar (logs, idempotência, decisões).</summary>
/// <param name="Propriedades">Application properties da mensagem recebida (inclui as gravadas ao abandonar).</param>
public sealed record ContextoDaMensagem(
    string MessageId,
    string? CorrelationId,
    int DeliveryCount,
    DateTimeOffset EnqueuedTime,
    string? SessionId = null,
    IReadOnlyDictionary<string, object>? Propriedades = null);

/// <summary>Motivos de dead-letter usados pelo consumidor (o broker usa os dele, ex.: <c>MaxDeliveryCountExceeded</c>).</summary>
public static class MotivosDeDeadLetter
{
    /// <summary>Corpo/contrato inválido: não adianta tentar de novo.</summary>
    public const string MensagemInvalida = "MensagemInvalida";

    /// <summary>Motivo que o BROKER grava ao exceder o MaxDeliveryCount.</summary>
    public const string MaxDeliveryCountExceeded = "MaxDeliveryCountExceeded";

    /// <summary>Motivo que o BROKER grava quando o TTL expira com DeadLetteringOnMessageExpiration = true.</summary>
    public const string TtlExpirado = "TTLExpiredException";
}
