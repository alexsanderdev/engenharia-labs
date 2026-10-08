namespace F6M04.Notificacoes.Contratos;

/// <summary>
/// PRONTO. Como Notificações ENXERGA os eventos de Pedidos (tolerant reader): só os campos de que precisa.
/// Campos novos no evento não quebram este consumidor; campos removidos, sim — por isso contratos são versionados.
/// </summary>
public sealed record PedidoCriadoRecebido(Guid PedidoId, string Numero, string ClienteEmail, decimal Total)
{
    public const string Tipo = "pedido.criado";
}

/// <summary>PRONTO. Ver <see cref="PedidoCriadoRecebido"/>.</summary>
public sealed record PedidoConfirmadoRecebido(Guid PedidoId, string Numero, string ClienteEmail)
{
    public const string Tipo = "pedido.confirmado";
}

/// <summary>PRONTO. Uma entrega do broker, já sem detalhes de protocolo.</summary>
/// <param name="MessageId">Identidade da mensagem (a chave de deduplicação). Vem da Outbox do publicador.</param>
/// <param name="Tipo">Nome do evento (<c>type</c> da mensagem AMQP).</param>
/// <param name="Corpo">JSON do evento.</param>
public sealed record MensagemRecebida(string MessageId, string Tipo, string Corpo);

/// <summary>Resultado de processar uma entrega.</summary>
public enum ResultadoDoProcessamento
{
    /// <summary>Primeira vez: efeito aplicado e MessageId registrado na Inbox.</summary>
    Processada,

    /// <summary>MessageId já estava na Inbox: reconhecida (ack) sem repetir o efeito.</summary>
    Duplicada,

    /// <summary>Tipo que este consumidor não trata: registrada na Inbox e ignorada.</summary>
    Ignorada,
}

/// <summary>PRONTO. A mensagem não pode ser processada nunca (JSON inválido, campo obrigatório ausente): vai para a DLQ.</summary>
public sealed class MensagemInvalidaException(string mensagem, Exception? interna = null) : Exception(mensagem, interna);
