namespace F1M04.Linq;

/// <summary>
/// Consultas em memória sobre pedidos.
/// </summary>
public static class ConsultasPedidos
{
    /// <summary>Soma dos subtotais dos itens do pedido.</summary>
    public static decimal Total(Pedido pedido)
    {
        throw new NotImplementedException("TODO: some os Subtotal dos itens");
    }

    /// <summary>
    /// Faturamento por cliente (ClienteId → soma dos totais), ignorando pedidos cancelados.
    /// Use <c>AggregateBy</c> (.NET 9+) para agrupar e somar em uma passada, sem criar grupos.
    /// </summary>
    public static IReadOnlyDictionary<int, decimal> FaturamentoPorCliente(IEnumerable<Pedido> pedidos)
    {
        throw new NotImplementedException("TODO: Where(não cancelado) + AggregateBy(ClienteId, 0m, ...) + ToDictionary");
    }

    /// <summary>
    /// Resumo por cliente: junta pedidos (não cancelados) com clientes (<c>Join</c>), agrupa por cliente
    /// (<c>GroupBy</c>) e ordena pelo total gasto, do maior para o menor (empate: nome).
    /// Clientes sem pedidos não aparecem.
    /// </summary>
    public static IReadOnlyList<ResumoCliente> ResumoPorCliente(IEnumerable<Pedido> pedidos, IEnumerable<Cliente> clientes)
    {
        throw new NotImplementedException("TODO: Join pedidos x clientes, GroupBy cliente, projete ResumoCliente e ordene");
    }

    /// <summary>
    /// Os <paramref name="top"/> produtos mais vendidos (soma das quantidades nos pedidos não cancelados).
    /// Use <c>SelectMany</c> para "achatar" os itens, agrupe por produto e junte com o catálogo para obter o nome.
    /// Empate na quantidade: ordem alfabética do nome.
    /// </summary>
    public static IReadOnlyList<ProdutoVendido> MaisVendidos(IEnumerable<Pedido> pedidos, IEnumerable<Produto> produtos, int top)
    {
        throw new NotImplementedException("TODO: SelectMany(Itens) + GroupBy(ProdutoId) + Join(produtos) + OrderByDescending + ThenBy + Take");
    }
}
