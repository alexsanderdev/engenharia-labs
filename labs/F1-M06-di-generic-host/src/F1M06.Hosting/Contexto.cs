namespace F1M06.Hosting;

/// <summary>
/// Dados da operação corrente (em uma API seria a requisição HTTP; num worker, um ciclo).
/// Vive exatamente um escopo → Scoped.
/// </summary>
public interface IContextoDaOperacao
{
    Guid Id { get; }
}

/// <inheritdoc />
public sealed class ContextoDaOperacao : IContextoDaOperacao
{
    public Guid Id { get; } = Guid.NewGuid();
}
