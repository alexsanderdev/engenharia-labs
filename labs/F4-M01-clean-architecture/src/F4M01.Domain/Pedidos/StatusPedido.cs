namespace F4M01.Domain.Pedidos;

/// <summary>Created → Confirmed → Completed; Created → Cancelled.</summary>
public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}
