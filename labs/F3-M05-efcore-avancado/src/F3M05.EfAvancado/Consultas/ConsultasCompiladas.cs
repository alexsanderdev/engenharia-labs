using F3M05.EfAvancado.Dominio;
using F3M05.EfAvancado.Persistencia;

namespace F3M05.EfAvancado.Consultas;

/// <summary>
/// Consultas quentes (chamadas milhares de vezes por minuto) compiladas UMA vez:
/// o EF pula a etapa de tradução LINQ → SQL a cada chamada.
/// </summary>
public static class ConsultasCompiladas
{
    /// <summary>
    /// Pedido (não excluído) com os itens, sem tracking, pelo Id; <c>null</c> se não existir.
    /// Deve ser criada com <c>EF.CompileAsyncQuery</c>.
    /// </summary>
    public static readonly Func<LojaDbContext, int, CancellationToken, Task<Pedido?>> PedidoComItensPorId =
        (db, id, ct) => throw new NotImplementedException(
            "TODO (Passo 6): EF.CompileAsyncQuery((LojaDbContext db, int id, CancellationToken ct) => " +
            "db.Pedidos.AsNoTracking().Include(p => p.Itens).FirstOrDefault(p => p.Id == id)).");
}
