using F4M04.Cqrs.Abstractions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace F4M04.Cqrs.Tests.Infra;

// Mensagens e handlers que existem SÓ nos testes, para observar o pipeline por dentro.
// Cada peça escreve no log; como o FakeLogCollector guarda a ordem, o teste enxerga
// exatamente em que sequência decorators, validator, handler e commit rodaram.
// Atenção: estes tipos são registrados por LISTA (AddCqrs(IEnumerable<Type>)), não por varredura
// deste assembly, porque aqui também existem handlers duplicados de propósito.

public sealed record ComandoDeTeste(string Texto) : ICommand<string>;

public sealed record OutroComandoDeTeste(int Numero) : ICommand<string>;

public sealed record QueryDeTeste : IQuery<int>;

public sealed record QuerySemHandler : IQuery<int>;

public sealed record ComandoDuplicado : ICommand<Unit>;

public sealed class ComandoDeTesteHandler(ILogger<ComandoDeTesteHandler> logger) : ICommandHandler<ComandoDeTeste, string>
{
    public Task<string> HandleAsync(ComandoDeTeste command, CancellationToken ct)
    {
        logger.LogInformation("Handler de ComandoDeTeste");
        if (command.Texto == "explodir") throw new InvalidOperationException("Falha simulada no handler.");
        return Task.FromResult(command.Texto.ToUpperInvariant());
    }
}

public sealed class ValidadorDeComandoDeTeste : AbstractValidator<ComandoDeTeste>
{
    public ValidadorDeComandoDeTeste(ILogger<ValidadorDeComandoDeTeste> logger)
    {
        RuleFor(c => c.Texto).Custom((_, _) => logger.LogInformation("Validando ComandoDeTeste"));
        RuleFor(c => c.Texto).NotEmpty().WithMessage("Texto é obrigatório.");
    }
}

public sealed class OutroComandoDeTesteHandler : ICommandHandler<OutroComandoDeTeste, string>
{
    public Task<string> HandleAsync(OutroComandoDeTeste command, CancellationToken ct) =>
        Task.FromResult($"outro:{command.Numero}");
}

public sealed class QueryDeTesteHandler(ILogger<QueryDeTesteHandler> logger) : IQueryHandler<QueryDeTeste, int>
{
    public Task<int> HandleAsync(QueryDeTeste query, CancellationToken ct)
    {
        logger.LogInformation("Handler de QueryDeTeste");
        return Task.FromResult(42);
    }
}

public sealed class HandlerDuplicadoA : ICommandHandler<ComandoDuplicado, Unit>
{
    public Task<Unit> HandleAsync(ComandoDuplicado command, CancellationToken ct) => Task.FromResult(Unit.Value);
}

public sealed class HandlerDuplicadoB : ICommandHandler<ComandoDuplicado, Unit>
{
    public Task<Unit> HandleAsync(ComandoDuplicado command, CancellationToken ct) => Task.FromResult(Unit.Value);
}

/// <summary>Substitui a unidade de trabalho real: só registra "Commit" no log.</summary>
public sealed class UnitOfWorkEspiao(ILogger<UnitOfWorkEspiao> logger) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct)
    {
        logger.LogInformation("Commit");
        return Task.CompletedTask;
    }
}

public static class TiposDeTeste
{
    public static readonly Type[] Pipeline =
    [
        typeof(ComandoDeTesteHandler),
        typeof(ValidadorDeComandoDeTeste),
        typeof(OutroComandoDeTesteHandler),
        typeof(QueryDeTesteHandler),
    ];

    public static readonly Type[] Duplicados = [typeof(HandlerDuplicadoA), typeof(HandlerDuplicadoB)];
}
