namespace F6M08.Consistencia.Dominio;

/// <summary>Ciclo de vida do pedido no OrderFlow (Criado → Confirmado; Criado → Cancelado).</summary>
public enum StatusPedido
{
    Criado,
    Confirmado,
    Cancelado,
}

/// <summary>
/// Evento publicado pela fonte da verdade (o agregado Pedido) depois de cada gravação.
/// <para>
/// <see cref="Versao"/> é a posição do evento na história DO AGREGADO: 1 para <see cref="PedidoCriado"/>,
/// 2 para o próximo evento do mesmo pedido, e assim por diante, sem buracos. É ela que permite ao
/// consumidor detectar duplicata (versão já vista), evento antigo (versão menor) e lacuna (versão
/// maior que a próxima esperada). Não existe ordem global entre pedidos diferentes — e não precisa.
/// </para>
/// </summary>
public abstract record EventoDePedido(Guid PedidoId, Guid ClienteId, long Versao, DateTimeOffset OcorridoEm);

/// <summary>Versão 1 de todo pedido. Carrega o estado inicial.</summary>
public sealed record PedidoCriado(Guid PedidoId, Guid ClienteId, long Versao, DateTimeOffset OcorridoEm, decimal Total)
    : EventoDePedido(PedidoId, ClienteId, Versao, OcorridoEm);

/// <summary>
/// Evento <b>delta</b>: soma <see cref="Valor"/> ao total. Aplicar duas vezes, ou pular um, deixa o total errado —
/// é o tipo de evento que torna a ordem e a deduplicação obrigatórias.
/// </summary>
public sealed record ItemAdicionado(Guid PedidoId, Guid ClienteId, long Versao, DateTimeOffset OcorridoEm, decimal Valor)
    : EventoDePedido(PedidoId, ClienteId, Versao, OcorridoEm);

public sealed record PedidoConfirmado(Guid PedidoId, Guid ClienteId, long Versao, DateTimeOffset OcorridoEm)
    : EventoDePedido(PedidoId, ClienteId, Versao, OcorridoEm);

public sealed record PedidoCancelado(Guid PedidoId, Guid ClienteId, long Versao, DateTimeOffset OcorridoEm, string Motivo)
    : EventoDePedido(PedidoId, ClienteId, Versao, OcorridoEm);
