namespace F2M08.Application.Abstracoes;

/// <summary>Contrato de todo caso de uso que altera estado. Convenção: implementações terminam com "Handler" e são sealed.</summary>
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}
