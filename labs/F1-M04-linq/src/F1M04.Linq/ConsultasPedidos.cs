namespace F1M04.Linq;

/// <summary>
/// Consultas em memória sobre pedidos.
/// </summary>
public static class ConsultasPedidos
{
    /// <summary>Soma dos subtotais dos itens do pedido.</summary>
    public static decimal Total(Pedido pedido)
    {
        return pedido.Itens.Sum(i => i.Subtotal);
    }

    /// <summary>
    /// Faturamento por cliente (ClienteId → soma dos totais), ignorando pedidos cancelados.
    /// Use <c>AggregateBy</c> (.NET 9+) para agrupar e somar em uma passada, sem criar grupos.
    /// </summary>
    public static IReadOnlyDictionary<int, decimal> FaturamentoPorCliente(IEnumerable<Pedido> pedidos)
    {
        return pedidos
            .Where(p => p.Status != StatusPedido.Cancelado)
            .AggregateBy(p => p.ClienteId, 0m, (acumulado, p) => acumulado + Total(p))
            .ToDictionary();
    }

    /// <summary>
    /// Resumo por cliente: junta pedidos (não cancelados) com clientes (<c>Join</c>), agrupa por cliente
    /// (<c>GroupBy</c>) e ordena pelo total gasto, do maior para o menor (empate: nome).
    /// Clientes sem pedidos não aparecem.
    /// </summary>
    public static IReadOnlyList<ResumoCliente> ResumoPorCliente(IEnumerable<Pedido> pedidos, IEnumerable<Cliente> clientes)
    {
        return pedidos
            .Where(p => p.Status != StatusPedido.Cancelado)
            .Join(clientes, p => p.ClienteId, c => c.Id, (p, c) => new { Cliente = c, Pedido = p })
            .GroupBy(x => x.Cliente)
            .Select(g => new ResumoCliente(g.Key.Nome, g.Count(), g.Sum(x => Total(x.Pedido))))
            .OrderByDescending(r => r.TotalGasto)
            .ThenBy(r => r.NomeCliente, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Os <paramref name="top"/> produtos mais vendidos (soma das quantidades nos pedidos não cancelados).
    /// Use <c>SelectMany</c> para "achatar" os itens, agrupe por produto e junte com o catálogo para obter o nome.
    /// Empate na quantidade: ordem alfabética do nome.
    /// </summary>
    public static IReadOnlyList<ProdutoVendido> MaisVendidos(IEnumerable<Pedido> pedidos, IEnumerable<Produto> produtos, int top)
    {
        return pedidos
            .Where(p => p.Status != StatusPedido.Cancelado)
            .SelectMany(p => p.Itens)
            .GroupBy(i => i.ProdutoId, (produtoId, itens) => new { ProdutoId = produtoId, Quantidade = itens.Sum(i => i.Quantidade) })
            .Join(produtos, v => v.ProdutoId, p => p.Id, (v, p) => new ProdutoVendido(p.Nome, v.Quantidade))
            .OrderByDescending(v => v.Quantidade)
            .ThenBy(v => v.Nome, StringComparer.Ordinal)
            .Take(top)
            .ToList();
    }
}
