using F3M05.EfAvancado.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace F3M05.EfAvancado.Persistencia.Interceptadores;

/// <summary>
/// Preenche as datas de <see cref="IAuditavel"/> antes de gravar, usando o <see cref="TimeProvider"/>:
/// <list type="bullet">
/// <item>Added: <c>CriadoEm</c> e <c>AtualizadoEm</c> = agora.</item>
/// <item>Modified: <c>AtualizadoEm</c> = agora e <c>CriadoEm</c> NUNCA é regravado.</item>
/// </list>
/// Precisa funcionar em <c>SaveChanges</c> e em <c>SaveChangesAsync</c>.
/// Atenção: <c>ExecuteUpdate</c>/<c>ExecuteDelete</c> NÃO passam por aqui.
/// </summary>
public sealed class AuditoriaInterceptor(TimeProvider relogio) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        // TODO (Passo 1): chame Auditar(eventData.Context) antes de seguir.
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        // TODO (Passo 1): chame Auditar(eventData.Context) antes de seguir.
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Auditar(DbContext? db)
    {
        _ = relogio;
        throw new NotImplementedException(
            "TODO (Passo 1): percorra db.ChangeTracker.Entries<IAuditavel>(); Added → CriadoEm e AtualizadoEm = agora; " +
            "Modified → AtualizadoEm = agora e Property(\"CriadoEm\").IsModified = false.");
    }
}
