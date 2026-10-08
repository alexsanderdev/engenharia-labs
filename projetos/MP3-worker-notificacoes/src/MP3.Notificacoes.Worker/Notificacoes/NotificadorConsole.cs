namespace MP3.Notificacoes.Worker.Notificacoes;

/// <summary>O provedor mais simples possível: escreve na saída padrão. Ótimo para desenvolvimento.</summary>
public sealed class NotificadorConsole : INotificador
{
    private readonly TextWriter saida;

    public NotificadorConsole() : this(Console.Out) { }

    public NotificadorConsole(TextWriter saida) => this.saida = saida;

    public string Canal => "console";

    public async Task EnviarAsync(Notificacao notificacao, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notificacao);
        ct.ThrowIfCancellationRequested();
        await saida.WriteLineAsync($"[notificação] para {notificacao.Destino}: {notificacao.Assunto} — {notificacao.Mensagem}");
    }
}
