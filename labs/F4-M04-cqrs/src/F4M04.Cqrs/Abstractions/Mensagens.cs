namespace F4M04.Cqrs.Abstractions;

/// <summary>
/// Marca uma intenção de MUDAR o estado do sistema ("faça isto").
/// Um command tem exatamente um handler e pode devolver um resultado mínimo
/// (o id criado, por exemplo), nunca um read model completo.
/// </summary>
/// <typeparam name="TResult">Resultado do command. Use <see cref="Unit"/> quando não houver.</typeparam>
public interface ICommand<TResult>;

/// <summary>
/// Marca uma PERGUNTA ao sistema ("me diga isto"). Uma query nunca altera estado
/// e é atendida pelo lado de leitura (read model).
/// </summary>
/// <typeparam name="TResult">Formato da resposta (DTO/read model).</typeparam>
public interface IQuery<TResult>;

/// <summary>Executa um command. Existe exatamente um handler por tipo de command.</summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

/// <summary>Responde uma query. Existe exatamente um handler por tipo de query.</summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}

/// <summary>"Void" que pode ser usado como argumento genérico (ex.: <c>ICommand&lt;Unit&gt;</c>).</summary>
public readonly record struct Unit
{
    public static readonly Unit Value;
}
