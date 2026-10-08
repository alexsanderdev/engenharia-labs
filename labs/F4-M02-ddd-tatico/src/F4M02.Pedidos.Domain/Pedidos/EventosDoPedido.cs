using F4M02.Pedidos.Domain.Comum;
using F4M02.Pedidos.Domain.ValueObjects;

namespace F4M02.Pedidos.Domain.Pedidos;

// PRONTO — os eventos são só dados. Quem decide QUANDO registrá-los é a raiz Pedido.

/// <summary>Um pedido novo foi aberto (status Created, ainda sem itens).</summary>
public sealed record PedidoCriado(PedidoId PedidoId, ClienteId ClienteId, DateTimeOffset OcorridoEm) : IEventoDeDominio;

/// <summary>O cliente confirmou o pedido: itens e total ficam congelados.</summary>
public sealed record PedidoConfirmado(PedidoId PedidoId, ClienteId ClienteId, Dinheiro Total, int QuantidadeDeItens, DateTimeOffset OcorridoEm) : IEventoDeDominio;

/// <summary>O pedido foi cancelado antes de ser confirmado.</summary>
public sealed record PedidoCancelado(PedidoId PedidoId, ClienteId ClienteId, string Motivo, DateTimeOffset OcorridoEm) : IEventoDeDominio;
