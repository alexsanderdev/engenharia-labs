namespace F1M02.Colecoes;

/// <summary>
/// Fila da cozinha: pedidos expressos saem antes dos normais, que saem antes dos agendados.
/// Dentro da mesma prioridade, a ordem de chegada (FIFO) deve ser respeitada.
/// </summary>
/// <remarks>
/// <see cref="PriorityQueue{TElement, TPriority}"/> NÃO é estável: elementos com a mesma prioridade
/// podem sair em qualquer ordem. Use uma prioridade composta (prioridade, sequência) para desempatar.
/// </remarks>
public sealed class FilaDePreparo
{
    private readonly PriorityQueue<Guid, (int Prioridade, long Sequencia)> _fila = new();
    private long _sequencia;

    /// <summary>Quantidade de pedidos aguardando.</summary>
    public int Quantidade => _fila.Count;

    /// <summary>Enfileira um pedido com a prioridade informada.</summary>
    public void Enfileirar(Guid pedidoId, PrioridadeDePreparo prioridade) =>
        _fila.Enqueue(pedidoId, ((int)prioridade, _sequencia++));

    /// <summary>
    /// Retira o próximo pedido. Retorna <c>false</c> se a fila estiver vazia.
    /// </summary>
    public bool TentarRetirar(out Guid pedidoId) => _fila.TryDequeue(out pedidoId, out _);
}
