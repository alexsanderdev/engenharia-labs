namespace F6M04.Notificacoes.Inbox;

/// <summary>
/// PRONTO. Registro de que ESTE consumidor já processou ESTA mensagem. Chave primária composta
/// (<see cref="MessageId"/>, <see cref="Consumidor"/>): a mesma mensagem pode (e deve) ser processada uma vez
/// por cada consumidor diferente — notificações, estoque, fidelidade...
/// </summary>
public sealed class InboxMessage
{
    private InboxMessage() { } // EF Core

    public InboxMessage(string messageId, string consumidor, string tipo, DateTimeOffset processadaEm)
    {
        MessageId = messageId;
        Consumidor = consumidor;
        Tipo = tipo;
        ProcessadaEm = processadaEm;
    }

    public string MessageId { get; private set; } = "";

    public string Consumidor { get; private set; } = "";

    public string Tipo { get; private set; } = "";

    public DateTimeOffset ProcessadaEm { get; private set; }
}
