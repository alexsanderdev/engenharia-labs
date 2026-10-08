using System.Collections.Concurrent;
using F4M04.Cqrs.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace F4M04.Cqrs.Dispatching;

/// <summary>
/// Dispatcher próprio (sem MediatR). Recebe a mensagem como <c>ICommand&lt;TResult&gt;</c>,
/// descobre o tipo concreto em tempo de execução e resolve no DI o
/// <c>ICommandHandler&lt;TipoConcreto, TResult&gt;</c> (que, por causa do AddCqrs, já vem
/// embrulhado pelos decorators do pipeline).
/// </summary>
public sealed class Dispatcher(IServiceProvider provider) : IDispatcher
{
    // Um "invocador" genérico por tipo de mensagem, criado UMA vez via reflexão e reutilizado:
    // depois disso a chamada é uma chamada virtual comum, sem MethodInfo.Invoke.
    private static readonly ConcurrentDictionary<Type, object> Invocadores = new();

    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var invocador = (Invocador<TResult>)Invocadores.GetOrAdd(
            command.GetType(),
            static tipo => Activator.CreateInstance(
                typeof(InvocadorDeCommand<,>).MakeGenericType(tipo, typeof(TResult)))!);
        return invocador.InvocarAsync(command, provider, ct);
    }

    public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var invocador = (Invocador<TResult>)Invocadores.GetOrAdd(
            query.GetType(),
            static tipo => Activator.CreateInstance(
                typeof(InvocadorDeQuery<,>).MakeGenericType(tipo, typeof(TResult)))!);
        return invocador.InvocarAsync(query, provider, ct);
    }

    private abstract class Invocador<TResult>
    {
        public abstract Task<TResult> InvocarAsync(object mensagem, IServiceProvider provider, CancellationToken ct);
    }

    private sealed class InvocadorDeCommand<TCommand, TResult> : Invocador<TResult>
        where TCommand : ICommand<TResult>
    {
        public override Task<TResult> InvocarAsync(object mensagem, IServiceProvider provider, CancellationToken ct)
        {
            var handler = provider.GetService<ICommandHandler<TCommand, TResult>>()
                ?? throw new InvalidOperationException(
                    $"Nenhum handler registrado para o command {typeof(TCommand).Name}. Ele está no assembly passado ao AddCqrs?");
            return handler.HandleAsync((TCommand)mensagem, ct);
        }
    }

    private sealed class InvocadorDeQuery<TQuery, TResult> : Invocador<TResult>
        where TQuery : IQuery<TResult>
    {
        public override Task<TResult> InvocarAsync(object mensagem, IServiceProvider provider, CancellationToken ct)
        {
            var handler = provider.GetService<IQueryHandler<TQuery, TResult>>()
                ?? throw new InvalidOperationException(
                    $"Nenhum handler registrado para a query {typeof(TQuery).Name}. Ela está no assembly passado ao AddCqrs?");
            return handler.HandleAsync((TQuery)mensagem, ct);
        }
    }
}
