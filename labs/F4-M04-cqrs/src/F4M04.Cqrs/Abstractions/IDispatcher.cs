namespace F4M04.Cqrs.Abstractions;

/// <summary>
/// Ponto único de entrada para commands e queries: quem chama não conhece o handler,
/// só a mensagem. É o "mediator" do lab, sem biblioteca externa.
/// </summary>
public interface IDispatcher
{
    /// <summary>Envia um command para o seu handler (passando pelo pipeline de decorators).</summary>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);

    /// <summary>Envia uma query para o seu handler (passando pelo pipeline de decorators).</summary>
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}
