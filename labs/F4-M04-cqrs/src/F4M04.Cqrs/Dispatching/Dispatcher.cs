using F4M04.Cqrs.Abstractions;

namespace F4M04.Cqrs.Dispatching;

/// <summary>
/// Dispatcher próprio (sem MediatR). Recebe a mensagem como <c>ICommand&lt;TResult&gt;</c>,
/// descobre o tipo concreto em tempo de execução e resolve no DI o
/// <c>ICommandHandler&lt;TipoConcreto, TResult&gt;</c> (que, por causa do AddCqrs, já vem
/// embrulhado pelos decorators do pipeline).
/// </summary>
public sealed class Dispatcher(IServiceProvider provider) : IDispatcher
{
    /// <summary>
    /// Resolve <c>ICommandHandler&lt;command.GetType(), TResult&gt;</c> no <c>provider</c> e chama HandleAsync.
    /// Sem handler registrado: <see cref="InvalidOperationException"/> com o nome do command na mensagem.
    /// </summary>
    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        _ = provider;
        throw new NotImplementedException(
            "TODO: feche ICommandHandler<,> com o tipo concreto do command (MakeGenericType), resolva no provider e chame HandleAsync. Sem handler: InvalidOperationException com o nome do command.");
    }

    /// <summary>
    /// Resolve <c>IQueryHandler&lt;query.GetType(), TResult&gt;</c> no <c>provider</c> e chama HandleAsync.
    /// Sem handler registrado: <see cref="InvalidOperationException"/> com o nome da query na mensagem.
    /// </summary>
    public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO: igual ao SendAsync, mas com IQueryHandler<,>. Sem handler: InvalidOperationException com o nome da query.");
}
