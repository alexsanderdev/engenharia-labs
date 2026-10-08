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
    public async Task<int> ObterEstoqueAsync(int produtoId, TimeSpan esperaMaxima, CancellationToken ct = default)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);

        // SET LOCK_TIMEOUT não aceita variável: o valor vai no texto (é um int, sem risco de injeção).
        // E não pode ir num comando com parâmetros (sp_executesql desfaz o SET ao terminar), por
        // isso o SET e o SELECT vão no MESMO batch. Alternativa: dois comandos, o SET sem parâmetros.
        var ms = (int)esperaMaxima.TotalMilliseconds;
        await using var comando = new SqlCommand(
            $"""
            SET LOCK_TIMEOUT {ms};
            SELECT Estoque FROM dbo.Produtos WHERE Id = @id;
            """, conexao);
        comando.Parameters.AddWithValue("@id", produtoId);

        try
        {
            return (int)(await comando.ExecuteScalarAsync(ct))!;
        }
        catch (SqlException ex) when (ex.Number == NumeroDoErroDeLockTimeout)
        {
            throw new RecursoBloqueadoException(
                $"O produto {produtoId} está bloqueado por outra transação há mais de {ms} ms.", ex);
        }
    }
}
