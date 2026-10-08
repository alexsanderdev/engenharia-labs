using System.Net.Mail;
using Microsoft.Extensions.Options;
using MP3.Notificacoes.Worker.Configuracao;

namespace MP3.Notificacoes.Worker.Notificacoes;

/// <summary>
/// E-mail via <see cref="SmtpClient"/>:
/// <list type="bullet">
/// <item><see cref="ModoSmtp.PastaDeSaida"/> (padrão e testes): o próprio SmtpClient grava cada e-mail como .eml numa pasta.</item>
/// <item><see cref="ModoSmtp.Rede"/>: envia de verdade — aponte para o smtp4dev do docker compose e veja na UI dele.</item>
/// </list>
/// Para produção a Microsoft recomenda MailKit ou um serviço (Azure Communication Services, SendGrid): a interface não muda.
/// </summary>
public sealed class NotificadorEmailSmtp(IOptions<SmtpOptions> opcoes) : INotificador
{
    public const string CabecalhoDeIdempotencia = "X-Idempotency-Key";

    public string Canal => "email";

    public async Task EnviarAsync(Notificacao notificacao, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notificacao);
        var o = opcoes.Value;

        MailAddress para;
        try
        {
            para = new MailAddress(notificacao.Destino);
        }
        catch (FormatException ex)
        {
            throw new FalhaPermanenteException($"E-mail inválido: '{notificacao.Destino}'.", ex);
        }

        using var mensagem = new MailMessage(new MailAddress(o.Remetente), para);
        mensagem.Subject = notificacao.Assunto;
        mensagem.Body = notificacao.Mensagem;
        // Provedores sérios deduplicam por uma chave: mandamos o EventoId para o caso de um reenvio escapar da inbox.
        mensagem.Headers.Add(CabecalhoDeIdempotencia, notificacao.EventoId.ToString());

        using var smtp = CriarCliente(o);
        try
        {
            await smtp.SendMailAsync(mensagem, ct);
        }
        catch (SmtpFailedRecipientException ex) when (ex.StatusCode is SmtpStatusCode.MailboxUnavailable
                                                       or SmtpStatusCode.MailboxNameNotAllowed
                                                       or SmtpStatusCode.UserNotLocalTryAlternatePath)
        {
            throw new FalhaPermanenteException($"Destinatário recusado: {ex.FailedRecipient} ({ex.StatusCode}).", ex);
        }
    }

    private static SmtpClient CriarCliente(SmtpOptions o)
    {
        if (o.Modo == ModoSmtp.Rede) return new SmtpClient(o.Host, o.Porta);

        var pasta = Path.GetFullPath(o.PastaDeSaida);
        Directory.CreateDirectory(pasta);
        return new SmtpClient
        {
            DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
            PickupDirectoryLocation = pasta,
        };
    }
}
