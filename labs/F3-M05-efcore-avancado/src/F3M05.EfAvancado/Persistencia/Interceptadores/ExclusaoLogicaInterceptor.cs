using F3M05.EfAvancado.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace F3M05.EfAvancado.Persistencia.Interceptadores;

/// <summary>
/// Transforma <c>Remove(entidade)</c> de um <see cref="IExclusaoLogica"/> em UPDATE:
/// a entrada Deleted passa a Modified com <c>Excluido = true</c> (só essa coluna é alterada;
/// o <see cref="AuditoriaInterceptor"/>, registrado depois, atualiza o <c>AtualizadoEm</c>).
/// </summary>
public sealed class ExclusaoLogicaInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        MarcarComoExcluido(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        MarcarComoExcluido(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void MarcarComoExcluido(DbContext? db)
    {
        if (db is null) return;

        foreach (var entrada in db.ChangeTracker.Entries<IExclusaoLogica>().ToList())
        {
            if (entrada.State != EntityState.Deleted) continue;

            entrada.State = EntityState.Unchanged;
            entrada.Entity.Excluido = true;
            entrada.Property(nameof(IExclusaoLogica.Excluido)).IsModified = true;
        }
    }
}
