using F3M05.EfAvancado.Dominio;
using F3M05.EfAvancado.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace F3M05.EfAvancado.Consultas;

public sealed class RelatorioPedidos(LojaDbContext db)
{
    /// <summary>
    /// Resumo de todos os pedidos (não excluídos), ordenado por Id: nome do cliente,
    /// quantidade de itens (linhas) e total (Σ quantidade × preço unitário).
    /// Meta: UM único comando SQL, qualquer que seja o número de pedidos.
    /// </summary>
    public async Task<IReadOnlyList<PedidoResumo>> ListarResumosAsync(CancellationToken ct = default)
    {
        // Projeção: o SQL traz só as 4 colunas, com JOIN no cliente e subconsultas de COUNT/SUM.
        return await db.Pedidos
            .OrderBy(p => p.Id)
            .Select(p => new PedidoResumo(
                p.Id,
                p.Cliente.Nome,
                p.Itens.Count,
                p.Itens.Sum(i => i.Quantidade * i.PrecoUnitario)))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Faturamento por dia a partir de <paramref name="desde"/> (pedidos não excluídos e não
    /// cancelados), ordenado por dia. Escrito em SQL com <c>Database.SqlQuery</c>: a agregação
    /// (GROUP BY) acontece no banco, em UM comando, e o parâmetro é seguro (interpolação vira
    /// parâmetro, não concatenação).
    /// </summary>
    public async Task<IReadOnlyList<FaturamentoDiario>> FaturamentoPorDiaAsync(DateTimeOffset desde, CancellationToken ct = default)
    {
        var cancelado = nameof(StatusPedido.Cancelled);
        return await db.Database.SqlQuery<FaturamentoDiario>(
            $"""
            SELECT CAST(p.CriadoEm AS date) AS Dia,
                   COUNT(DISTINCT p.Id)              AS Pedidos,
                   SUM(i.Quantidade * i.PrecoUnitario) AS Total
            FROM Pedidos p
            JOIN ItensPedido i ON i.PedidoId = p.Id
            WHERE p.Excluido = 0 AND p.Status <> {cancelado} AND p.CriadoEm >= {desde}
            GROUP BY CAST(p.CriadoEm AS date)
            """)
            .OrderBy(f => f.Dia)
            .ToListAsync(ct);
    }
}
