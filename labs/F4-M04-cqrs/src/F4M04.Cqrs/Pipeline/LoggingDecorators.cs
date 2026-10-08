using F4M04.Cqrs.Abstractions;
using Microsoft.Extensions.Logging;

namespace F4M04.Cqrs.Pipeline;

/// <summary>
/// Mensagens de log do pipeline (PRONTAS; source generator do LoggerMessage: sem boxing nem parse do template a cada chamada).
/// Os testes procuram por "Executando {Nome}", "{Nome} concluído em X ms" e "{Nome} falhou".
/// </summary>
internal static partial class LogDoPipeline
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Executando {Mensagem}")]
    public static partial void Executando(ILogger logger, string mensagem);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Mensagem} concluído em {Duracao} ms")]
    public static partial void Concluido(ILogger logger, string mensagem, long duracao);

    /// <summary>Calcula a duração só depois de saber que o log está ligado.</summary>
    public static void Concluido(ILogger logger, string mensagem, TimeProvider relogio, long inicio)
    {
        if (!logger.IsEnabled(LogLevel.Information)) return;
        var duracao = (long)relogio.GetElapsedTime(inicio).TotalMilliseconds;
        Concluido(logger, mensagem, duracao);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "{Mensagem} falhou")]
    public static partial void Falhou(ILogger logger, Exception ex, string mensagem);
}

/// <summary>
/// Decorator MAIS EXTERNO dos commands: registra início, fim (com duração) e falha.
/// Como fica por fora, também registra falhas de validação e de commit.
/// </summary>
public sealed class LoggingCommandDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ILogger<LoggingCommandDecorator<TCommand, TResult>> logger,
    TimeProvider relogio) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Loga "Executando {Nome}", chama o inner, loga "{Nome} concluído em X ms".
    /// Se o inner lançar, loga "{Nome} falhou" (nível Error, com a exceção) e relança.
    /// {Nome} é <c>typeof(TCommand).Name</c>. Use os métodos de <see cref="LogDoPipeline"/>.
    /// </summary>
    public Task<TResult> HandleAsync(TCommand command, CancellationToken ct)
    {
        _ = (inner, logger, relogio);
        throw new NotImplementedException(
            "TODO: LogDoPipeline.Executando → await inner.HandleAsync → LogDoPipeline.Concluido; em exceção, LogDoPipeline.Falhou e relance (throw;).");
    }
}

/// <summary>Mesmo comportamento do <see cref="LoggingCommandDecorator{TCommand,TResult}"/>, para queries.</summary>
public sealed class LoggingQueryDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    ILogger<LoggingQueryDecorator<TQuery, TResult>> logger,
    TimeProvider relogio) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public Task<TResult> HandleAsync(TQuery query, CancellationToken ct)
    {
        _ = (inner, logger, relogio);
        throw new NotImplementedException("TODO: igual ao LoggingCommandDecorator, com typeof(TQuery).Name.");
    }
}
