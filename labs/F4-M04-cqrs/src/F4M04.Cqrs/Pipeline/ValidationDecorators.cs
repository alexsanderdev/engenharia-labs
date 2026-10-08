using F4M04.Cqrs.Abstractions;
using FluentValidation;

namespace F4M04.Cqrs.Pipeline;

/// <summary>
/// Valida o command com todos os <see cref="IValidator{T}"/> registrados.
/// Se houver erro, lança <see cref="ValidacaoException"/> (nome = <c>typeof(TCommand).Name</c>,
/// erros agrupados por PropertyName) e NÃO chama o inner (curto-circuito):
/// o handler e a unidade de trabalho nem ficam sabendo do command inválido.
/// Sem validators registrados, apenas repassa.
/// </summary>
public sealed class ValidationCommandDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public Task<TResult> HandleAsync(TCommand command, CancellationToken ct)
    {
        _ = (inner, validators);
        throw new NotImplementedException(
            "TODO: rode ValidateAsync de cada validator, junte os Errors; se houver algum, lance ValidacaoException com um dicionário PropertyName → mensagens; senão chame o inner.");
    }
}

/// <summary>Mesmo comportamento do <see cref="ValidationCommandDecorator{TCommand,TResult}"/>, para queries.</summary>
public sealed class ValidationQueryDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public Task<TResult> HandleAsync(TQuery query, CancellationToken ct)
    {
        _ = (inner, validators);
        throw new NotImplementedException("TODO: igual ao ValidationCommandDecorator (extraia a lógica comum para um helper).");
    }
}
