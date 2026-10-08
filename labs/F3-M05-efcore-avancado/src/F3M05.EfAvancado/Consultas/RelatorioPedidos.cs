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
        // CONSULTA RUIM (N+1): o resultado está certo, mas são 1 + 2 × N comandos.
        // Com 5 pedidos: 11 round-trips. Com 5.000: 10.001.
        // TODO (Passo 4): troque tudo por UMA projeção (Select direto para PedidoResumo).
        var pedidos = await db.Pedidos.OrderBy(p => p.Id).ToListAsync(ct);

        var resumos = new List<PedidoResumo>();
        foreach (var pedido in pedidos)
        {
            await db.Entry(pedido).Reference(p => p.Cliente).LoadAsync(ct);
            await db.Entry(pedido).Collection(p => p.Itens).LoadAsync(ct);
            resumos.Add(new PedidoResumo(
                pedido.Id,
                pedido.Cliente.Nome,
                pedido.Itens.Count,
                pedido.Itens.Sum(i => i.Quantidade * i.PrecoUnitario)));
        }
        return resumos;
    }

    /// <summary>
    /// Faturamento por dia a partir de <paramref name="desde"/> (pedidos não excluídos e não
    /// cancelados), ordenado por dia. Escrito em SQL com <c>Database.SqlQuery</c>: a agregação
    /// (GROUP BY) acontece no banco, em UM comando, e o parâmetro é seguro (interpolação vira
    /// parâmetro, não concatenação).
    /// </summary>
    public Task<IReadOnlyList<FaturamentoDiario>> FaturamentoPorDiaAsync(DateTimeOffset desde, CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "TODO (Passo 7): db.Database.SqlQuery<FaturamentoDiario>($\"SELECT CAST(p.CriadoEm AS date) AS Dia, ... GROUP BY ...\")" +
            ".OrderBy(f => f.Dia).ToListAsync(ct). Use interpolação ({desde}) — vira parâmetro.");
    }
}
