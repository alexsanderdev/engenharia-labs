namespace F6M04.Pedidos.Mensageria;

/// <summary>PRONTO. Mensagem pronta para sair para o broker.</summary>
/// <param name="MessageId">Identidade estável (a mesma em toda republicação): chave de deduplicação do consumidor.</param>
/// <param name="Tipo">Nome do evento (ex.: <c>pedido.criado</c>); vira a routing key.</param>
/// <param name="ChaveDeOrdenacao">Agregado de origem (o consumidor pode usar para particionar/ordenar).</param>
/// <param name="Payload">Corpo JSON.</param>
/// <param name="OcorridoEm">Quando o fato aconteceu.</param>
public sealed record MensagemDeSaida(Guid MessageId, string Tipo, string ChaveDeOrdenacao, string Payload, DateTimeOffset OcorridoEm);

/// <summary>
/// PRONTO. Porta de saída para o broker. O contrato é: quando <see cref="PublicarAsync"/> retorna sem exceção,
/// o broker CONFIRMOU que aceitou e roteou a mensagem (publisher confirm). Qualquer dúvida vira exceção.
/// </summary>
public interface IPublicadorDeMensagens
{
    Task PublicarAsync(MensagemDeSaida mensagem, CancellationToken ct = default);
}
