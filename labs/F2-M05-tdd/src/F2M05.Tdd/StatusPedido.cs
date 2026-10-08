namespace F2M05.Tdd;

/// <summary>
/// Estados possíveis de um pedido no OrderFlow.
/// Transições válidas: Created → Confirmed → Completed; Created → Cancelled.
/// </summary>
public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}
