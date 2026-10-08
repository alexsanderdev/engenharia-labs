namespace F2M08.Domain.Entidades;

/// <summary>Created → Confirmed → Completed; Created → Cancelled. Completed não cancela.</summary>
public enum StatusPedido
{
    Created = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3,
}
