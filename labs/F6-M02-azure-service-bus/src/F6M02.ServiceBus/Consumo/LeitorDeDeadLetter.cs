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
#pragma warning disable CS9113 // TODO (Passo 5): "cliente" passa a ser usado quando você implementar os métodos (apague este pragma).
public sealed class LeitorDeDeadLetter(ServiceBusClient cliente)
#pragma warning restore CS9113
{
    /// <summary>Propriedade carimbada na mensagem reenviada: quantas vezes ela já voltou da DLQ.</summary>
    public const string PropriedadeReenvios = "reenviosDaDlq";

    /// <summary>
    /// Lê até <paramref name="maximo"/> mensagens da DLQ de <paramref name="origem"/> SEM removê-las
    /// (<c>PeekMessagesAsync</c> num receiver com <c>SubQueue = SubQueue.DeadLetter</c>).
    /// </summary>
    public Task<IReadOnlyList<MensagemMorta>> InspecionarAsync(
        OrigemDasMensagens origem, int maximo = 100, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 5): receiver de origem.CriarReceiver(cliente, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter }) " +
            "com await using; PeekMessagesAsync(maximo) e Converter cada uma.");

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
    public Task<int> ReenviarAsync(
        string fila, int maximo, TimeSpan esperaMaxima, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 5): receiver da DLQ + sender da fila; ReceiveMessagesAsync(..., esperaMaxima); para cada uma: " +
            "new ServiceBusMessage(morta), remova DeadLetterReason/DeadLetterErrorDescription, incremente PropriedadeReenvios, " +
            "envie e SÓ ENTÃO complete na DLQ.");

    private static MensagemMorta Converter(ServiceBusReceivedMessage m) =>
        new(m.MessageId, m.DeadLetterReason, m.DeadLetterErrorDescription, m.DeliveryCount, m.SequenceNumber, m.Body.ToString());
}
