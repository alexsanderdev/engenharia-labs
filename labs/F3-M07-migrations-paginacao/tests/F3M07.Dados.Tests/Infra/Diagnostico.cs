using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace F3M07.Dados.Tests.Infra;

/// <summary>Já vem pronto. Consultas ao catálogo do SQL Server e medição de leituras lógicas.</summary>
public static partial class Diagnostico
{
    public sealed record Coluna(string Nome, string Tipo, bool AceitaNull, string? Default);

    /// <summary>Colunas da tabela (INFORMATION_SCHEMA). rowversion aparece com DATA_TYPE "timestamp".</summary>
    public static async Task<IReadOnlyList<Coluna>> ColunasAsync(string connectionString, string tabela, CancellationToken ct)
    {
        const string sql = """
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @Tabela
            ORDER BY ORDINAL_POSITION
            """;
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var comando = new SqlCommand(sql, conexao);
        comando.Parameters.AddWithValue("@Tabela", tabela);
        await using var leitor = await comando.ExecuteReaderAsync(ct);

        var colunas = new List<Coluna>();
        while (await leitor.ReadAsync(ct))
        {
            colunas.Add(new Coluna(
                leitor.GetString(0),
                leitor.GetString(1),
                leitor.GetString(2) == "YES",
                leitor.IsDBNull(3) ? null : leitor.GetString(3)));
        }
        return colunas;
    }

    public static async Task<T?> EscalarAsync<T>(string connectionString, string sql, CancellationToken ct)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var comando = new SqlCommand(sql, conexao);
        var valor = await comando.ExecuteScalarAsync(ct);
        return valor is null or DBNull ? default : (T)valor;
    }

    public static async Task ExecutarAsync(string connectionString, string sql, CancellationToken ct)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var comando = new SqlCommand(sql, conexao) { CommandTimeout = 120 };
        await comando.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Executa o SQL (ex.: saída de <c>IQueryable.ToQueryString()</c>) com <c>SET STATISTICS IO ON</c>
    /// e devolve a soma das "logical reads" (páginas de 8 KB lidas do buffer) de todas as tabelas.
    /// </summary>
    public static async Task<int> LeiturasLogicasAsync(string connectionString, string sql, CancellationToken ct)
    {
        var total = 0;
        await using var conexao = new SqlConnection(connectionString);
        conexao.InfoMessage += (_, e) =>
        {
            foreach (SqlError mensagem in e.Errors)
            foreach (Match m in LogicalReads().Matches(mensagem.Message))
                total += int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        };
        await conexao.OpenAsync(ct);

        await using (var ligar = new SqlCommand("SET STATISTICS IO ON;", conexao))
            await ligar.ExecuteNonQueryAsync(ct);

        await using (var comando = new SqlCommand(sql, conexao))
        await using (var leitor = await comando.ExecuteReaderAsync(ct))
        {
            // Consome TODOS os result sets: as mensagens de STATISTICS IO chegam junto com o fluxo de resultados.
            do
            {
                while (await leitor.ReadAsync(ct)) { }
            } while (await leitor.NextResultAsync(ct));
        }
        return total;
    }

    [GeneratedRegex(@"logical reads (\d+)")]
    private static partial Regex LogicalReads();
}
