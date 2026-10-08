namespace F6M01.Mensageria.Contratos;

/// <summary>Toda mensagem que trafega pelo broker. (PRONTO)</summary>
public interface IMensagem
{
}

/// <summary>
/// COMANDO: um pedido para que UM destinatário específico faça algo. Nome no imperativo
/// (<c>ReservarEstoque</c>). Pode ser recusado. Quem envia sabe quem deve executar.
/// </summary>
public interface IComando : IMensagem
{
}

/// <summary>
/// EVENTO: um fato que JÁ aconteceu, publicado para N interessados que o publicador não conhece.
/// Nome no passado (<c>PedidoCriado</c>). Não pode ser recusado: no máximo ignorado.
/// </summary>
public interface IEvento : IMensagem
{
}

/// <summary>Comando para o módulo de Estoque reservar os itens de um pedido. (PRONTO)</summary>
public sealed record ReservarEstoque(Guid PedidoId, IReadOnlyList<ItemDaReserva> Itens) : IComando;

/// <summary>Item de uma reserva de estoque. (PRONTO)</summary>
public sealed record ItemDaReserva(string Sku, int Quantidade);

/// <summary>Evento: um pedido foi criado no OrderFlow. Versão 1 do contrato. (PRONTO)</summary>
public sealed record PedidoCriado(Guid PedidoId, Guid ClienteId, decimal Total, DateTimeOffset CriadoEm) : IEvento;

/// <summary>Evento: um pedido foi cancelado. Versão 1 do contrato. (PRONTO)</summary>
public sealed record PedidoCancelado(Guid PedidoId, string Motivo) : IEvento;
