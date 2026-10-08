namespace F6M03.Kafka.Contratos;

/// <summary>
/// Todo evento do agregado Pedido. A <see cref="PedidoId"/> vira a CHAVE da mensagem no Kafka:
/// é ela que garante que os eventos do mesmo pedido caiam na mesma partição (e, portanto, em ordem).
/// </summary>
public interface IEventoDePedido
{
    /// <summary>Identidade do evento (não do pedido): é o que o consumidor idempotente deduplica.</summary>
    Guid EventoId { get; }

    /// <summary>Pedido ao qual o evento se refere (chave de partição).</summary>
    Guid PedidoId { get; }

    /// <summary>Quando o fato aconteceu no produtor.</summary>
    DateTimeOffset OcorridoEm { get; }
}

/// <summary>Contrato v1: um pedido foi criado no OrderFlow.</summary>
public sealed record PedidoCriado(
    Guid EventoId,
    Guid PedidoId,
    Guid ClienteId,
    decimal Total,
    DateTimeOffset OcorridoEm) : IEventoDePedido;

/// <summary>Contrato v1: um pedido foi confirmado (sempre depois de <see cref="PedidoCriado"/>).</summary>
public sealed record PedidoConfirmado(
    Guid EventoId,
    Guid PedidoId,
    DateTimeOffset OcorridoEm) : IEventoDePedido;

/// <summary>
/// A mensagem não pode ser entendida por este consumidor (tipo desconhecido, versão de contrato
/// não suportada, JSON inválido). Não adianta tentar de novo: é uma "poison message" e vai direto para a DLQ.
/// </summary>
public sealed class ContratoNaoSuportadoException(string mensagem, Exception? interna = null)
    : Exception(mensagem, interna);
