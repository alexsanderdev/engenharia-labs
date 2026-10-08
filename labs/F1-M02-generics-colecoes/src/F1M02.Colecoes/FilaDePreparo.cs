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
    // TODO: declare aqui a PriorityQueue e um contador de sequência.

    /// <summary>Quantidade de pedidos aguardando.</summary>
    public int Quantidade =>
        throw new NotImplementedException("TODO: retorne a quantidade de itens da fila");

    /// <summary>Enfileira um pedido com a prioridade informada.</summary>
    public void Enfileirar(Guid pedidoId, PrioridadeDePreparo prioridade) =>
        throw new NotImplementedException("TODO: enfileire com prioridade composta ((int)prioridade, sequencia++)");

    /// <summary>
    /// Retira o próximo pedido. Retorna <c>false</c> se a fila estiver vazia.
    /// </summary>
    public bool TentarRetirar(out Guid pedidoId) =>
        throw new NotImplementedException("TODO: use TryDequeue");
}
