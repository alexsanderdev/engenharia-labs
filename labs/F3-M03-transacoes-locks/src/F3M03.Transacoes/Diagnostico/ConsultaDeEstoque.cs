using Microsoft.Data.SqlClient;

namespace F3M03.Transacoes.Diagnostico;

/// <summary>
/// A leitura não conseguiu o lock dentro do tempo máximo (erro 1222 do SQL Server,
/// "Lock request time out period exceeded"). A <see cref="Exception.InnerException"/> é a SqlException original.
/// </summary>
public sealed class RecursoBloqueadoException : Exception
{
    public RecursoBloqueadoException() { }
    public RecursoBloqueadoException(string message) : base(message) { }
    public RecursoBloqueadoException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Leitura de estoque que prefere falhar rápido a ficar presa atrás de um bloqueio.</summary>
public sealed class ConsultaDeEstoque(string connectionString)
{
    /// <summary>Número do erro "Lock request time out period exceeded".</summary>
    public const int NumeroDoErroDeLockTimeout = 1222;

    /// <summary>
    /// Lê o estoque do produto esperando no máximo <paramref name="esperaMaxima"/> por locks
    /// (<c>SET LOCK_TIMEOUT</c>, em milissegundos, na MESMA sessão, antes do SELECT).
    /// Se estourar, lança <see cref="RecursoBloqueadoException"/> com a SqlException 1222 como InnerException.
    /// </summary>
    public Task<int> ObterEstoqueAsync(int produtoId, TimeSpan esperaMaxima, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 8): no MESMO batch do SELECT, 'SET LOCK_TIMEOUT <ms>;' (valor literal, é um int) e depois " +
            "'SELECT Estoque FROM dbo.Produtos WHERE Id = @id;'. Capture SqlException com Number == " +
            "NumeroDoErroDeLockTimeout (1222) e lance RecursoBloqueadoException com ela como InnerException. " +
            $"(connection string com {connectionString.Length} caracteres)");
}
