namespace F4M06.Clientes.Contracts;

/// <summary>
/// Contrato público SÍNCRONO de Clientes. Pedidos usa para validar o cliente antes de criar um pedido.
/// Repare: Pedidos depende de <c>Clientes.Contracts</c> e Clientes depende de <c>Pedidos.Contracts</c>
/// (para assinar <c>PedidoConfirmado</c>) — sem ciclo entre os projetos de implementação.
/// </summary>
public interface IClientesApi
{
    /// <summary>O cliente existe (está cadastrado)?</summary>
    Task<bool> ExisteAsync(Guid clienteId, CancellationToken ct = default);
}
