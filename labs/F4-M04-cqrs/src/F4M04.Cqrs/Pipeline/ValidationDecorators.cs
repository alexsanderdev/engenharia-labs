using F4M04.Cqrs.Abstractions;
using FluentValidation;

namespace F4M04.Cqrs.Pipeline;

/// <summary>Lógica comum de validação: roda TODOS os validators e agrupa os erros por propriedade.</summary>
internal static class Validacao
{
    public static async Task ValidarAsync<T>(IEnumerable<IValidator<T>> validators, T mensagem, CancellationToken ct)
    {
        var falhas = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var resultado = await validator.ValidateAsync(mensagem, ct).ConfigureAwait(false);
            falhas.AddRange(resultado.Errors);
        }

        if (falhas.Count == 0) return;

        var erros = falhas
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());
        throw new ValidacaoException(typeof(T).Name, erros);
    }
}

/// <summary>
/// Valida o command com todos os <see cref="IValidator{T}"/> registrados.
/// Se houver erro, lança <see cref="ValidacaoException"/> e NÃO chama o inner (curto-circuito):
/// o handler e a unidade de trabalho nem ficam sabendo do command inválido.
/// Sem validators registrados, apenas repassa.
/// </summary>
public sealed class ValidationCommandDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken ct)
    {
        await Validacao.ValidarAsync(validators, command, ct).ConfigureAwait(false);
        return await inner.HandleAsync(command, ct).ConfigureAwait(false);
    }
}

/// <summary>Mesmo comportamento do <see cref="ValidationCommandDecorator{TCommand,TResult}"/>, para queries.</summary>
public sealed class ValidationQueryDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> HandleAsync(TQuery query, CancellationToken ct)
    {
        await Validacao.ValidarAsync(validators, query, ct).ConfigureAwait(false);
        return await inner.HandleAsync(query, ct).ConfigureAwait(false);
    }
}
