using Microsoft.Data.SqlClient;

namespace F3M03.Transacoes.Tests.Infra;

/// <summary>
/// PRONTA. Uma sessão do SQL Server (uma conexão aberta, sem pool) que os testes de demonstração
/// usam como se fosse uma aba do SSMS: <c>BEGIN TRAN</c>, <c>SELECT</c>, <c>COMMIT</c> em T-SQL puro.
/// Assim a demonstração lê como um roteiro de SQL, com duas "abas" (A e B) intercaladas.
/// </summary>
public sealed class Sessao : IAsyncDisposable
{
    /// <summary>Rede de segurança: nenhum comando de teste espera lock por mais que isto.</summary>
    public const int LockTimeoutPadraoMs = 15_000;

    private readonly SqlConnection _conexao;

    private Sessao(string nome, SqlConnection conexao, int spid)
    {
        Nome = nome;
        _conexao = conexao;
        Spid = spid;
    }

    /// <summary>Nome para mensagens ("A", "B"...).</summary>
    public string Nome { get; }

    /// <summary>session_id (@@SPID) desta sessão no SQL Server.</summary>
    public int Spid { get; }

    public static async Task<Sessao> AbrirAsync(string connectionString, string nome)
    {
        var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var cmd = new SqlCommand($"SET LOCK_TIMEOUT {LockTimeoutPadraoMs}; SELECT @@SPID;", conexao);
        var spid = Convert.ToInt32(await cmd.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
        return new Sessao(nome, conexao, spid);
    }

    /// <summary>Executa um batch T-SQL (sem parâmetros: SETs valem para a sessão).</summary>
    public async Task ExecutarAsync(string sql)
    {
        await using var cmd = new SqlCommand(sql, _conexao) { CommandTimeout = 30 };
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Executa e devolve a primeira coluna da primeira linha.</summary>
    public async Task<T> EscalarAsync<T>(string sql)
    {
        await using var cmd = new SqlCommand(sql, _conexao) { CommandTimeout = 30 };
        var valor = await cmd.ExecuteScalarAsync();
        return (T)Convert.ChangeType(valor!, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Executa um batch que DEVE falhar e devolve a SqlException.</summary>
    public async Task<SqlException> FalharAsync(string sql)
    {
        try
        {
            await ExecutarAsync(sql);
        }
        catch (SqlException ex)
        {
            return ex;
        }
        throw new InvalidOperationException($"A sessão {Nome} deveria ter falhado ao executar: {sql}");
    }

    /// <summary>Quantas transações abertas a sessão tem (@@TRANCOUNT).</summary>
    public Task<int> TranCountAsync() => EscalarAsync<int>("SELECT @@TRANCOUNT;");

    public async ValueTask DisposeAsync() => await _conexao.DisposeAsync();
}
