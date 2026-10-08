namespace F6M04.Notificacoes.Dominio;

/// <summary>
/// PRONTO. O EFEITO COLATERAL do consumidor: uma notificação registrada na "caixa de saída" de e-mails.
/// Duplicar esta linha = cliente recebe dois e-mails iguais (o bug que a Inbox evita).
/// </summary>
public sealed class Notificacao
{
    private Notificacao() { } // EF Core

    public Notificacao(Guid pedidoId, string tipo, string destinatario, string texto, DateTimeOffset criadaEm)
    {
        Id = Guid.CreateVersion7(criadaEm);
        PedidoId = pedidoId;
        Tipo = tipo;
        Destinatario = destinatario;
        Texto = texto;
        CriadaEm = criadaEm;
    }

    public Guid Id { get; private set; }

    public Guid PedidoId { get; private set; }

    public string Tipo { get; private set; } = "";

    public string Destinatario { get; private set; } = "";

    public string Texto { get; private set; } = "";

    public DateTimeOffset CriadaEm { get; private set; }
}
