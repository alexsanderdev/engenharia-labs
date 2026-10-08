namespace F6M05.Sagas.Retry;

/// <summary>
/// PRONTO. Quantas vezes e com que espera repetir uma mensagem que falhou com erro transitório.
/// </summary>
/// <remarks>
/// Dois níveis, como em praticamente todo framework de mensageria:
/// <list type="number">
/// <item><b>Imediato</b>: repete na hora, dentro da mesma entrega (resolve soluços de
/// milissegundos: deadlock, conexão do pool que caiu). Barato, mas segura a mensagem e o
/// consumidor.</item>
/// <item><b>Atrasado</b>: devolve a mensagem ao broker numa <b>fila de espera</b> com TTL; quando
/// o TTL vence, o broker a manda de volta para a fila principal (dead-letter exchange). Resolve
/// falhas de segundos/minutos (dependência reiniciando) sem segurar o consumidor.</item>
/// </list>
/// Total de execuções no pior caso: <c>(1 + RetentativasImediatas) × (1 + Atrasos.Count)</c>.
/// Depois disso, a mensagem vai para a DLQ.
/// </remarks>
public sealed record PoliticaDeRetry
{
    /// <summary>Retentativas imediatas por entrega (0 = sem retry imediato).</summary>
    public int RetentativasImediatas { get; init; } = 2;

    /// <summary>
    /// Uma espera por nível de retry atrasado (ex.: 1 s, 10 s, 1 min). Cada nível vira uma fila
    /// de espera com o seu TTL fixo.
    /// </summary>
    public IReadOnlyList<TimeSpan> Atrasos { get; init; } = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1)];

    /// <summary>Prefetch do consumidor (mensagens entregues sem ack ao mesmo tempo).</summary>
    public ushort Prefetch { get; init; } = 10;

    /// <summary>Execuções do manipulador no pior caso antes da DLQ.</summary>
    public int TotalDeExecucoes => (1 + RetentativasImediatas) * (1 + Atrasos.Count);

    /// <summary>Fila de espera do nível <paramref name="nivel"/> (1 = primeira retentativa atrasada).</summary>
    public static string NomeDaFilaDeEspera(string fila, int nivel) => $"{fila}.espera.{nivel}";

    /// <summary>Dead-letter queue da fila.</summary>
    public static string NomeDaDlq(string fila) => $"{fila}.dlq";

    /// <summary>Exchange pelo qual as filas de espera devolvem mensagens para a fila principal.</summary>
    public static string NomeDoExchangeDeReentrada(string fila) => $"{fila}.reentrada";

    /// <summary>Valida a política (lança <see cref="ArgumentException"/>).</summary>
    public PoliticaDeRetry Validar()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(RetentativasImediatas);
        if (Atrasos.Any(a => a <= TimeSpan.Zero))
            throw new ArgumentException("Todo atraso precisa ser positivo.", nameof(Atrasos));
        if (Prefetch == 0)
            throw new ArgumentException("Prefetch 0 = sem limite; defina um valor.", nameof(Prefetch));
        return this;
    }
}
