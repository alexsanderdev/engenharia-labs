namespace F4M04.Cqrs.Abstractions;

/// <summary>
/// Confirma (commit) tudo o que um command alterou, de uma vez, e só então
/// publica os eventos de domínio gerados. Usado APENAS no pipeline de commands.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
