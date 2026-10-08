namespace F4M02.Pedidos.Domain.Descontos;

/// <summary>
/// O que a política de desconto precisa saber do cliente. Vem do contexto de Clientes (por consulta
/// ou por uma projeção local alimentada por eventos) — o agregado Pedido não conhece nada disso.
/// </summary>
/// <remarks>PRONTO.</remarks>
/// <param name="ClienteId">Cliente a quem o perfil se refere.</param>
/// <param name="PedidosConcluidos">Quantos pedidos o cliente já concluiu.</param>
/// <param name="Vip">Se o cliente é do programa VIP.</param>
public sealed record PerfilDoCliente(ClienteId ClienteId, int PedidosConcluidos, bool Vip);
