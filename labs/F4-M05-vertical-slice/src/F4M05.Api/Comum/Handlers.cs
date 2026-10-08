namespace F4M05.Api.Comum;

// Mini "mediator" do lab (sem MediatR: licença comercial desde a v13 e, para isto, desnecessário).
// Commands ALTERAM estado; queries só LEEM. Separar as duas interfaces é o CQRS "natural" das fatias:
// cada fatia já nasce sendo uma coisa ou outra.

/// <summary>Executa um comando (intenção de alterar estado) e devolve um resultado.</summary>
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

/// <summary>Executa uma consulta (sem efeitos colaterais).</summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}

/// <summary>Cada fatia publica as próprias rotas. Program.cs não precisa conhecer as features.</summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
