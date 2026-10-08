namespace F4M02.Pedidos.Domain.Pedidos;

/// <summary>
/// Ciclo de vida do pedido: Created → Confirmed → Completed; Created → Cancelled.
/// Completed e Cancelled são finais.
/// </summary>
/// <remarks>PRONTO.</remarks>
public enum StatusPedido
{
    Created = 1,
    Confirmed = 2,
    Completed = 3,
    Cancelled = 4,
}
