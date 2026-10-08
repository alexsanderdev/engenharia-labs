using Azure.Messaging.ServiceBus;

namespace F6M02.ServiceBus.Consumo;

/// <summary>Uma mensagem morta, do jeito que o time de operação precisa enxergar.</summary>
public sealed record MensagemMorta(
    string MessageId,
    string? Motivo,
    string? Descricao,
    int DeliveryCount,
    long SequenceNumber,
    string Corpo);

/// <summary>
/// Ferramentas de operação para a dead-letter queue (<c>SubQueue.DeadLetter</c>): inspecionar sem remover
/// e reenviar (redrive) para a fila de origem depois que a causa foi corrigida.
/// </summary>
public sealed class LeitorDeDeadLetter(ServiceBusClient cliente)
{
    /// <summary>Propriedade carimbada na mensagem reenviada: quantas vezes ela já voltou da DLQ.</summary>
    public const string PropriedadeReenvios = "reenviosDaDlq";

    /// <summary>
    /// Lê até <paramref name="maximo"/> mensagens da DLQ de <paramref name="origem"/> SEM removê-las
    /// (<c>PeekMessagesAsync</c> num receiver com <c>SubQueue = SubQueue.DeadLetter</c>).
    /// </summary>
    public async Task<IReadOnlyList<MensagemMorta>> InspecionarAsync(
        OrigemDasMensagens origem, int maximo = 100, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(origem);
        await using var receiver = origem.CriarReceiver(cliente, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
        var mensagens = await receiver.PeekMessagesAsync(maximo, fromSequenceNumber: null, ct);
        return [.. mensagens.Select(Converter)];
    }

    /// <summary>
    /// Reenvia para a FILA de origem até <paramref name="maximo"/> mensagens mortas:
    /// recebe da DLQ em peek-lock, envia uma cópia (<c>new ServiceBusMessage(recebida)</c>) sem as
    /// propriedades de dead-letter e com <see cref="PropriedadeReenvios"/> incrementada, e só então completa
    /// a mensagem na DLQ. Devolve quantas foram reenviadas.
    /// </summary>
    /// <remarks>
    /// Só para filas: reenviar para um TÓPICO entregaria a mensagem de novo a TODAS as subscriptions.
    /// Para subscription, reenvie para uma fila do consumidor ou use o ForwardTo.
    /// </remarks>
    public async Task<int> ReenviarAsync(
        string fila, int maximo, TimeSpan esperaMaxima, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fila);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximo, 1);

        await using var dlq = cliente.CreateReceiver(fila, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
        await using var sender = cliente.CreateSender(fila);

        var reenviadas = 0;
        while (reenviadas < maximo)
        {
            var lote = await dlq.ReceiveMessagesAsync(maxMessages: Math.Min(20, maximo - reenviadas), maxWaitTime: esperaMaxima, ct);
            if (lote.Count == 0) return reenviadas;

            foreach (var morta in lote)
            {
                var copia = new ServiceBusMessage(morta);
                copia.ApplicationProperties.Remove("DeadLetterReason");
                copia.ApplicationProperties.Remove("DeadLetterErrorDescription");
                var anteriores = copia.ApplicationProperties.TryGetValue(PropriedadeReenvios, out var v) ? Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture) : 0;
                copia.ApplicationProperties[PropriedadeReenvios] = anteriores + 1;

                // Envia ANTES de completar: se cair no meio, o pior caso é duplicata (consumidor idempotente), não perda.
                await sender.SendMessageAsync(copia, ct);
                await dlq.CompleteMessageAsync(morta, ct);
                reenviadas++;
            }
        }

        return reenviadas;
    }

    private static MensagemMorta Converter(ServiceBusReceivedMessage m) =>
        new(m.MessageId, m.DeadLetterReason, m.DeadLetterErrorDescription, m.DeliveryCount, m.SequenceNumber, m.Body.ToString());
}
