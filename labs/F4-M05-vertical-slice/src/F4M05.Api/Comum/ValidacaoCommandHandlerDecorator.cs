using FluentValidation;
using FluentValidation.Results;

namespace F4M05.Api.Comum;

/// <summary>
/// Decorator (pipeline behavior "caseiro"): roda TODOS os validadores do comando antes do handler.
/// O handler da fatia só é chamado com entrada válida e não precisa repetir validação.
/// </summary>
internal sealed class ValidacaoCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken ct)
    {
        var falhas = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var resultado = await validator.ValidateAsync(command, ct);
            falhas.AddRange(resultado.Errors);
        }

        if (falhas.Count > 0)
            throw new ValidationException(falhas);

        return await inner.HandleAsync(command, ct);
    }
}
