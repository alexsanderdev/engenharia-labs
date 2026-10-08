using Microsoft.Data.SqlClient;

namespace F3M03.Transacoes.Deadlocks;

/// <summary>
/// Reexecuta uma operação que foi escolhida como VÍTIMA de deadlock (SqlException número 1205).
/// </summary>
/// <remarks>
/// A "operação" tem que ser a transação INTEIRA (abrir conexão, BEGIN, comandos, COMMIT).
/// Quando a sessão é vítima, o SQL Server já desfez a transação toda: repetir só o último
/// comando não faz sentido.
/// </remarks>
/// <param name="politica">Máximo de tentativas e backoff.</param>
/// <param name="relogio">Fonte de tempo para as esperas (nos testes, <c>FakeTimeProvider</c>).</param>
/// <param name="ehRetentavel">
/// Quais exceções merecem nova tentativa. Padrão: <see cref="EhDeadlock"/>. É um ponto de extensão
/// (e uma "costura" para testar o backoff sem precisar fabricar uma SqlException).
/// </param>
public sealed class RetryDeDeadlock(PoliticaDeRetry politica, TimeProvider relogio, Func<Exception, bool>? ehRetentavel = null)
{
    /// <summary>Número do erro "Transaction was deadlocked ... and has been chosen as the deadlock victim".</summary>
    public const int NumeroDoErroDeDeadlock = 1205;

    private readonly Func<Exception, bool> _ehRetentavel = ehRetentavel ?? EhDeadlock;

    /// <summary>
    /// <c>true</c> se <paramref name="ex"/> é, ou embrulha em qualquer nível de <see cref="Exception.InnerException"/>
    /// (ex.: <c>DbUpdateException</c> do EF Core), uma <see cref="SqlException"/> com <c>Number == 1205</c>.
    /// </summary>
    public static bool EhDeadlock(Exception ex) =>
        throw new NotImplementedException(
            $"TODO (Passo 5): percorra ex, ex.InnerException, ... e devolva true se achar uma {nameof(SqlException)} " +
            $"com Number == {NumeroDoErroDeDeadlock}. (Recebido: {ex.GetType().Name})");

    /// <summary>
    /// Executa <paramref name="operacao"/> (recebe o número da tentativa: 1, 2, 3...).
    /// Se ela lançar uma exceção retentável e ainda houver tentativas, espera
    /// <see cref="PoliticaDeRetry.CalcularAtraso"/> usando o <c>relogio</c> e tenta de novo.
    /// Exceção não retentável, ou tentativas esgotadas: relança a exceção ORIGINAL (sem embrulhar).
    /// </summary>
    public Task<T> ExecutarAsync<T>(Func<int, CancellationToken, Task<T>> operacao, CancellationToken ct = default) =>
        throw new NotImplementedException(
            $"TODO (Passo 5): laço de tentativas (1..{politica.MaxTentativas}) com try/catch e filtro " +
            "'catch (Exception ex) when (tentativa < MaxTentativas && _ehRetentavel(ex))'; dentro do catch, " +
            $"'await Task.Delay(politica.CalcularAtraso(tentativa), relogio, ct)' (relógio: {relogio.GetType().Name}). " +
            $"Fora do filtro, a exceção sobe sozinha, intacta. (retentável: {_ehRetentavel.Method.Name})");
}
