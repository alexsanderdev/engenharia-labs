using System.Globalization;
using Microsoft.Data.SqlClient;

namespace F3M03.Transacoes.Diagnostico;

/// <summary>Uma sessão esperando um lock que outra sessão segura ("quem bloqueia quem").</summary>
/// <param name="SessaoBloqueada">session_id de quem está esperando.</param>
/// <param name="SessaoBloqueadora">session_id de quem segura o lock (blocking_session_id).</param>
/// <param name="TipoDeEspera">wait_type, ex.: <c>LCK_M_S</c>, <c>LCK_M_U</c>, <c>LCK_M_X</c>.</param>
/// <param name="TempoDeEsperaMs">Há quanto tempo está esperando (wait_time).</param>
/// <param name="TipoDeRecurso">resource_type do lock esperado: KEY, PAGE, RID, OBJECT...</param>
/// <param name="ModoSolicitado">request_mode do lock esperado: S, U, X, IX...</param>
/// <param name="Tabela">Nome da tabela do recurso (null se não for possível resolver).</param>
/// <param name="ComandoBloqueado">Texto do batch/comando da sessão bloqueada.</param>
public sealed record Bloqueio(
    int SessaoBloqueada,
    int SessaoBloqueadora,
    string TipoDeEspera,
    long TempoDeEsperaMs,
    string TipoDeRecurso,
    string ModoSolicitado,
    string? Tabela,
    string? ComandoBloqueado);

/// <summary>Um lock (concedido ou em espera) de uma sessão, como aparece em <c>sys.dm_tran_locks</c>.</summary>
/// <param name="TipoDeRecurso">resource_type: DATABASE, OBJECT, PAGE, KEY, RID...</param>
/// <param name="Modo">request_mode: S, U, X, IS, IX, SIX, RangeS-S, RangeS-U...</param>
/// <param name="Status">request_status: GRANT, WAIT ou CONVERT.</param>
/// <param name="Tabela">Nome da tabela (null para DATABASE).</param>
public sealed record LockAtivo(string TipoDeRecurso, string Modo, string Status, string? Tabela);

/// <summary>
/// Diagnóstico de bloqueio com DMVs. O C# está pronto: ele só executa os arquivos
/// <c>Sql/QuemBloqueiaQuem.sql</c> e <c>Sql/LocksDaSessao.sql</c> e lê as colunas pelo NOME.
/// O seu trabalho é escrever o T-SQL desses dois arquivos.
/// </summary>
/// <remarks>Exige a permissão <c>VIEW SERVER STATE</c> (o <c>sa</c> do container tem).</remarks>
public sealed class DiagnosticoDeBloqueio(string connectionString)
{
    /// <summary>Lista as sessões bloqueadas no banco atual, com quem as bloqueia e o que esperam.</summary>
    public async Task<IReadOnlyList<Bloqueio>> ListarBloqueiosAsync(CancellationToken ct = default)
    {
        var sql = await LerSqlAsync("QuemBloqueiaQuem.sql", ct);
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync(ct);

        var resultado = new List<Bloqueio>();
        while (await leitor.ReadAsync(ct))
        {
            resultado.Add(new Bloqueio(
                SessaoBloqueada: Convert.ToInt32(leitor["SessaoBloqueada"], CultureInfo.InvariantCulture),
                SessaoBloqueadora: Convert.ToInt32(leitor["SessaoBloqueadora"], CultureInfo.InvariantCulture),
                TipoDeEspera: Convert.ToString(leitor["TipoDeEspera"], CultureInfo.InvariantCulture) ?? "",
                TempoDeEsperaMs: Convert.ToInt64(leitor["TempoDeEsperaMs"], CultureInfo.InvariantCulture),
                TipoDeRecurso: Convert.ToString(leitor["TipoDeRecurso"], CultureInfo.InvariantCulture) ?? "",
                ModoSolicitado: Convert.ToString(leitor["ModoSolicitado"], CultureInfo.InvariantCulture) ?? "",
                Tabela: leitor["Tabela"] as string,
                ComandoBloqueado: leitor["ComandoBloqueado"] as string));
        }
        return resultado;
    }

    /// <summary>Lista os locks da sessão <paramref name="sessao"/> no banco atual.</summary>
    public async Task<IReadOnlyList<LockAtivo>> ListarLocksDaSessaoAsync(int sessao, CancellationToken ct = default)
    {
        var sql = await LerSqlAsync("LocksDaSessao.sql", ct);
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var comando = new SqlCommand(sql, conexao);
        comando.Parameters.AddWithValue("@sessao", sessao);
        await using var leitor = await comando.ExecuteReaderAsync(ct);

        var resultado = new List<LockAtivo>();
        while (await leitor.ReadAsync(ct))
        {
            resultado.Add(new LockAtivo(
                TipoDeRecurso: (Convert.ToString(leitor["TipoDeRecurso"], CultureInfo.InvariantCulture) ?? "").Trim(),
                Modo: (Convert.ToString(leitor["Modo"], CultureInfo.InvariantCulture) ?? "").Trim(),
                Status: (Convert.ToString(leitor["Status"], CultureInfo.InvariantCulture) ?? "").Trim(),
                Tabela: leitor["Tabela"] as string));
        }
        return resultado;
    }

    private static Task<string> LerSqlAsync(string arquivo, CancellationToken ct) =>
        File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", arquivo), ct);
}
