namespace MP3.Notificacoes.Worker.Notificacoes;

/// <summary>O que vai ser enviado, já montado. <see cref="EventoId"/> viaja junto como chave de idempotência do provedor.</summary>
public sealed record Notificacao(Guid EventoId, Guid PedidoId, string Canal, string Destino, string Assunto, string Mensagem);

/// <summary>
/// A estratégia de envio de um canal. Cada provedor (console, e-mail, SMS, push...) é uma implementação;
/// o worker não sabe qual está usando.
/// <para>
/// Contrato de erros: lance <see cref="FalhaPermanenteException"/> quando repetir NÃO vai ajudar
/// (destino inválido, conteúdo rejeitado). Qualquer outra exceção é tratada como transitória (retry com backoff).
/// Respeite o <see cref="CancellationToken"/>: ele dispara no desligamento do worker.
/// </para>
/// </summary>
public interface INotificador
{
    /// <summary>Nome do canal (case-insensitive): "console", "email", "sms"...</summary>
    string Canal { get; }

    Task EnviarAsync(Notificacao notificacao, CancellationToken ct);
}

/// <summary>Falha em que tentar de novo não resolve: a mensagem vai direto para a DLQ.</summary>
public sealed class FalhaPermanenteException : Exception
{
    public FalhaPermanenteException(string message) : base(message) { }

    public FalhaPermanenteException(string message, Exception innerException) : base(message, innerException) { }

    public FalhaPermanenteException() { }
}
