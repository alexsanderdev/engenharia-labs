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
        // TODO (Passo 3): chame MarcarComoExcluido(eventData.Context) antes de seguir.
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        // TODO (Passo 3): chame MarcarComoExcluido(eventData.Context) antes de seguir.
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void MarcarComoExcluido(DbContext? db)
    {
        throw new NotImplementedException(
            "TODO (Passo 3): para cada entrada Deleted de Entries<IExclusaoLogica>(): State = Unchanged, " +
            "Entity.Excluido = true e Property(\"Excluido\").IsModified = true.");
    }
}
