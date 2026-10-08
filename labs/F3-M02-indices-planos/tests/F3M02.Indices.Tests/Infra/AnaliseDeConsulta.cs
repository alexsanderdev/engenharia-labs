using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

namespace F3M02.Indices.Tests.Infra;

/// <summary>Um operador de acesso a dados do plano (Seek, Scan, Lookup) ou outro operador (Sort, Hash Match...).</summary>
/// <param name="Operador">Nome como aparece no plano gráfico (ex.: "Index Seek", "Key Lookup", "Clustered Index Scan").</param>
/// <param name="Tabela">Tabela acessada (sem colchetes), ou null para operadores que não leem tabela.</param>
/// <param name="Indice">Índice usado (sem colchetes), ou null.</param>
public sealed record OperadorDoPlano(string Operador, string? Tabela, string? Indice)
{
    public bool EhKeyLookup => Operador is "Key Lookup" or "RID Lookup";
    public bool EhSeek => Operador is "Index Seek" or "Clustered Index Seek";
    public bool EhScan => Operador is "Index Scan" or "Clustered Index Scan" or "Table Scan";
    public override string ToString() => Tabela is null ? Operador : $"{Operador} [{Tabela}].[{Indice}]";
}

/// <summary>
/// Executa uma consulta com <c>SET STATISTICS XML ON</c> (plano REAL, devolvido como um result set extra)
/// e <c>SET STATISTICS IO ON</c> (leituras lógicas por tabela, devolvidas como mensagens).
/// É exatamente o que você vê no SSMS com "Include Actual Execution Plan" + aba Messages.
/// </summary>
public sealed partial class AnaliseDeConsulta
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";

    private AnaliseDeConsulta(List<object?[]> linhas, List<XDocument> planos, Dictionary<string, int> leituras)
    {
        Linhas = linhas;
        Planos = planos;
        LeiturasPorTabela = leituras;
        Operadores = planos.SelectMany(p => p.Descendants(Ns + "RelOp")).Select(LerOperador).ToList();
        AvisosDeConversao = planos
            .SelectMany(p => p.Descendants(Ns + "PlanAffectingConvert"))
            .Select(a => $"{(string?)a.Attribute("ConvertIssue")}: {(string?)a.Attribute("Expression")}")
            .ToList();
    }

    /// <summary>Linhas devolvidas pela consulta (valores na ordem das colunas).</summary>
    public IReadOnlyList<object?[]> Linhas { get; }

    /// <summary>Planos reais em XML (um por instrução).</summary>
    public IReadOnlyList<XDocument> Planos { get; }

    /// <summary>Todos os operadores do plano, de cima para baixo.</summary>
    public IReadOnlyList<OperadorDoPlano> Operadores { get; }

    /// <summary>Avisos <c>PlanAffectingConvert</c> (conversão implícita que impede seek ou piora estimativa).</summary>
    public IReadOnlyList<string> AvisosDeConversao { get; }

    /// <summary>Leituras lógicas (páginas de 8 KB lidas do buffer) por tabela, do <c>STATISTICS IO</c>.</summary>
    public IReadOnlyDictionary<string, int> LeiturasPorTabela { get; }

    public int LeiturasLogicas => LeiturasPorTabela.Values.Sum();

    public IEnumerable<OperadorDoPlano> AcessosA(string tabela) => Operadores.Where(o => o.Tabela == tabela);

    public bool TemOperador(string operador) => Operadores.Any(o => o.Operador == operador);

    /// <summary>Resumo para mensagens de erro: o plano em uma linha.</summary>
    public string Resumo =>
        $"Plano: {string.Join(" → ", Operadores)} | Leituras lógicas: {string.Join(", ", LeiturasPorTabela.Select(kv => $"{kv.Key}={kv.Value}"))}";

    private static OperadorDoPlano LerOperador(XElement relOp)
    {
        var fisico = (string?)relOp.Attribute("PhysicalOp") ?? "?";
        // O acesso (IndexScan/TableScan) é filho DIRETO do RelOp; o resto do XML é dos operadores filhos.
        var acesso = relOp.Elements().FirstOrDefault(e => e.Name == Ns + "IndexScan" || e.Name == Ns + "TableScan");
        if (acesso is null) return new OperadorDoPlano(fisico, null, null);

        var objeto = acesso.Element(Ns + "Object");
        var tabela = ((string?)objeto?.Attribute("Table"))?.Trim('[', ']');
        var indice = ((string?)objeto?.Attribute("Index"))?.Trim('[', ']');
        // No XML o Key Lookup é um "Clustered Index Seek" com Lookup="1"; o SSMS o desenha como "Key Lookup".
        var lookup = (string?)acesso.Attribute("Lookup") is "1" or "true";
        return new OperadorDoPlano(lookup ? "Key Lookup" : fisico, tabela, indice);
    }

    /// <summary>Executa <paramref name="sql"/> (como o ADO.NET faz: <c>sp_executesql</c> com parâmetros) e coleta plano e leituras.</summary>
    public static async Task<AnaliseDeConsulta> ExecutarAsync(string connectionString, string sql, params SqlParameter[] parametros)
    {
        var leituras = new Dictionary<string, int>();
        await using var conexao = new SqlConnection(connectionString);
        conexao.InfoMessage += (_, e) =>
        {
            foreach (Match m in LeituraLogica().Matches(e.Message))
            {
                var tabela = m.Groups["tabela"].Value;
                leituras[tabela] = leituras.GetValueOrDefault(tabela) + int.Parse(m.Groups["leituras"].Value, CultureInfo.InvariantCulture);
            }
        };
        await conexao.OpenAsync();

        await using (var ligar = new SqlCommand("SET STATISTICS XML ON; SET STATISTICS IO ON;", conexao))
            await ligar.ExecuteNonQueryAsync();

        var linhas = new List<object?[]>();
        var planos = new List<XDocument>();
        await using (var cmd = new SqlCommand(sql, conexao))
        {
            cmd.Parameters.AddRange(parametros);
            await using var reader = await cmd.ExecuteReaderAsync();
            do
            {
                var ehPlano = reader.FieldCount == 1 && reader.GetName(0).Contains("Showplan", StringComparison.OrdinalIgnoreCase);
                while (await reader.ReadAsync())
                {
                    if (ehPlano)
                    {
                        planos.Add(XDocument.Parse(reader.GetString(0)));
                        continue;
                    }
                    var valores = new object?[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; i++)
                        valores[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    linhas.Add(valores);
                }
            } while (await reader.NextResultAsync());
        }

        return new AnaliseDeConsulta(linhas, planos, leituras);
    }

    [GeneratedRegex(@"Table '(?<tabela>[^']+)'\. Scan count \d+, logical reads (?<leituras>\d+)")]
    private static partial Regex LeituraLogica();
}
