namespace F3M04.EfCore.Dominio;

/// <summary>
/// Ciclo de vida do pedido: Created → Confirmed → Completed; Created → Cancelled.
/// No banco, o status é gravado como TEXTO ("Created", "Confirmed"...), não como número.
/// </summary>
public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}
