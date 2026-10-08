using System.Data;
using Microsoft.Data.SqlClient;

namespace F3M02.Indices.Tests.Infra;

/// <summary>Base dos testes: executa um arquivo de <c>Sql/Consultas/</c> depois de aplicar o seu <c>Indices.sql</c>.</summary>
public abstract class PlanoTestBase(SqlServerFixture fixture)
{
    protected SqlServerFixture Fixture { get; } = fixture;

    /// <summary>Aplica os índices (uma vez) e executa a consulta capturando plano real e leituras lógicas.</summary>
    protected async Task<AnaliseDeConsulta> AnalisarAsync(string arquivo, params SqlParameter[] parametros)
    {
        await Fixture.GarantirIndicesAsync();
        var analise = await AnaliseDeConsulta.ExecutarAsync(Fixture.ConnectionString, ScriptsSql.Ler($"Consultas/{arquivo}"), parametros);
        TestContext.Current.TestOutputHelper?.WriteLine(analise.Resumo);
        return analise;
    }

    /// <summary>Consulta auxiliar SEM captura de plano (para montar o resultado esperado ou ler o catálogo).</summary>
    protected async Task<List<object?[]>> ConsultarAsync(string sql)
    {
        await Fixture.GarantirIndicesAsync();
        await using var conexao = new SqlConnection(Fixture.ConnectionString);
        await conexao.OpenAsync(TestContext.Current.CancellationToken);
        await using var cmd = new SqlCommand(sql, conexao);
        await using var reader = await cmd.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var linhas = new List<object?[]>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            var valores = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
                valores[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            linhas.Add(valores);
        }
        return linhas;
    }

    protected static SqlParameter Inteiro(string nome, int valor) => new(nome, SqlDbType.Int) { Value = valor };

    /// <summary>Parâmetro NVARCHAR, o que o ADO.NET/Dapper/EF mandam por padrão para <c>string</c>.</summary>
    protected static SqlParameter TextoUnicode(string nome, string valor, int tamanho) =>
        new(nome, SqlDbType.NVarChar, tamanho) { Value = valor };
}
