using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace MP3.Notificacoes.Worker.Notificacoes;

/// <summary>
/// "Gateway de SMS" de mentira: valida o número (E.164) como um gateway real faria e guarda o que "enviou".
/// Número inválido é falha PERMANENTE: repetir não conserta o cadastro do cliente.
/// </summary>
public sealed partial class NotificadorSmsFake(ILogger<NotificadorSmsFake> logger) : INotificador
{
    private readonly ConcurrentQueue<Notificacao> enviados = new();

    public string Canal => "sms";

    public IReadOnlyCollection<Notificacao> Enviados => enviados;

    public Task EnviarAsync(Notificacao notificacao, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notificacao);
        ct.ThrowIfCancellationRequested();
        if (!E164().IsMatch(notificacao.Destino))
            throw new FalhaPermanenteException($"Telefone fora do formato E.164: '{notificacao.Destino}'.");

        // SMS tem limite de tamanho: um gateway real cobraria por segmento de 160 caracteres.
        var texto = notificacao.Mensagem.Length <= 160 ? notificacao.Mensagem : string.Concat(notificacao.Mensagem.AsSpan(0, 157), "...");
        enviados.Enqueue(notificacao with { Mensagem = texto });
        LogSmsEnviado(logger, notificacao.Destino, notificacao.PedidoId);
        return Task.CompletedTask;
    }

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164();

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS (fake) para {Destino} sobre o pedido {PedidoId}.")]
    private static partial void LogSmsEnviado(ILogger logger, string destino, Guid pedidoId);
}
