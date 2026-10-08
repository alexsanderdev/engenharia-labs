using F2M02.Solid.Dominio;

namespace F2M02.Solid.Aplicacao;

/// <summary>
/// Consulta que só LÊ pedidos. Por isso depende de <see cref="ILeitorDePedidos"/> (1 método),
/// e não de um "IPedidoRepository" gordo com salvar/excluir/relatório (ISP).
/// </summary>
public sealed class ResumoDeComprasDoCliente(ILeitorDePedidos leitor)
{
    /// <summary>Soma o <see cref="Pedido.Total"/> dos pedidos do cliente, ignorando os cancelados.</summary>
    public Task<decimal> TotalGastoAsync(Guid clienteId, CancellationToken ct)
    {
        _ = leitor;
        throw new NotImplementedException("TODO: liste os pedidos do cliente e some o Total dos que não estão Cancelled");
    }
}
