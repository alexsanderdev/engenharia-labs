using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using MP3.Notificacoes.Worker.Configuracao;

namespace MP3.Notificacoes.Worker.Mensageria;

/// <summary>
/// Comando operacional: devolve mensagens da DLQ para a fila principal (depois que a causa foi corrigida).
/// <c>dotnet run --project src/MP3.Notificacoes.Worker -- reprocessar-dlq [maximo]</c>
/// <para>
/// Cada mensagem é copiada (mesmo MessageId e corpo — a inbox continua protegendo contra duplicata),
/// perde o contador de tentativas e os metadados de dead-letter, ganha <c>mp3-reprocessada-de</c> com o motivo
/// original, é enviada à fila e só então completada na DLQ.
/// </para>
/// </summary>
public sealed partial class ReprocessadorDeDlq(ServiceBusClient cliente, IOptions<ServiceBusOptions> opcoes, ILogger<ReprocessadorDeDlq> logger)
{
    public const string PropriedadeReprocessada = "mp3-reprocessada-de";

    public async Task<int> ReprocessarAsync(int maximo = 100, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximo);
        var fila = opcoes.Value.Fila;
        await using var receiver = cliente.CreateReceiver(fila, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
        await using var sender = cliente.CreateSender(fila);

        var reprocessadas = 0;
        while (reprocessadas < maximo)
        {
            var lote = await receiver.ReceiveMessagesAsync(Math.Min(50, maximo - reprocessadas), TimeSpan.FromSeconds(2), ct);
            if (lote.Count == 0) break;

            foreach (var morta in lote)
            {
                var copia = new ServiceBusMessage(morta);
                copia.ApplicationProperties.Remove(ConsumidorDeNotificacoes.PropriedadeTentativa);
                copia.ApplicationProperties.Remove("DeadLetterReason");
                copia.ApplicationProperties.Remove("DeadLetterErrorDescription");
                copia.ApplicationProperties[PropriedadeReprocessada] = morta.DeadLetterReason ?? "desconhecido";

                await sender.SendMessageAsync(copia, ct);       // primeiro garante a cópia...
                await receiver.CompleteMessageAsync(morta, ct); // ...depois tira da DLQ
                reprocessadas++;
                LogReprocessada(logger, morta.MessageId, morta.DeadLetterReason);
            }
        }
        return reprocessadas;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Mensagem {MessageId} devolvida à fila (motivo original: {Motivo}).")]
    private static partial void LogReprocessada(ILogger logger, string messageId, string? motivo);
}
