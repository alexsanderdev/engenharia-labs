using F4M04.Cqrs.Abstractions;

namespace F4M04.Cqrs.Pipeline;

/// <summary>
/// Decorator MAIS INTERNO dos commands: chama o handler e, só se ele terminar sem exceção,
/// confirma a unidade de trabalho (<see cref="IUnitOfWork.SaveChangesAsync"/>).
/// Não existe versão para queries: query não grava nada.
/// </summary>
public sealed class UnitOfWorkCommandDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IUnitOfWork unitOfWork) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public Task<TResult> HandleAsync(TCommand command, CancellationToken ct)
    {
        _ = (inner, unitOfWork);
        throw new NotImplementedException("TODO: await inner.HandleAsync; depois await unitOfWork.SaveChangesAsync; devolva o resultado do inner.");
    }
}
