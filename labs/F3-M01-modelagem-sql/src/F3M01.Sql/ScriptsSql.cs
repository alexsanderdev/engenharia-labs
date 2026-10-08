using System.Text.RegularExpressions;

namespace F3M01.Sql;

/// <summary>
/// Lê os arquivos <c>.sql</c> do lab (copiados para o output em <c>Sql/</c>) e divide scripts em lotes.
/// Pronto: você não precisa alterar este arquivo; o seu trabalho está nos arquivos <c>.sql</c>.
/// </summary>
public static partial class ScriptsSql
{
    /// <summary>Pasta <c>Sql/</c> no diretório de saída (onde os testes rodam).</summary>
    public static string Pasta => Path.Combine(AppContext.BaseDirectory, "Sql");

    /// <summary>Lê um arquivo relativo à pasta <c>Sql/</c>, por exemplo <c>ParteA/Schema.sql</c>.</summary>
    public static string Ler(string caminhoRelativo)
    {
        var caminho = Path.Combine(Pasta, caminhoRelativo);
        if (!File.Exists(caminho))
            throw new FileNotFoundException($"Arquivo SQL não encontrado: Sql/{caminhoRelativo}. Ele existe em src/F3M01.Sql/Sql?", caminho);

        return File.ReadAllText(caminho);
    }

    /// <summary>
    /// Divide um script em lotes separados por linhas <c>GO</c> (como o SSMS e o sqlcmd fazem).
    /// <c>GO</c> não é T-SQL: é um separador das ferramentas, por isso o ADO.NET não o entende.
    /// Lotes que só têm comentários são descartados.
    /// </summary>
    public static IReadOnlyList<string> DividirEmLotes(string script) =>
        SeparadorGo().Split(script)
            .Where(lote => !string.IsNullOrWhiteSpace(RemoverComentarios(lote)))
            .ToList();

    private static string RemoverComentarios(string sql) =>
        ComentarioDeLinha().Replace(ComentarioDeBloco().Replace(sql, ""), "");

    [GeneratedRegex(@"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorGo();

    [GeneratedRegex(@"--[^\r\n]*")]
    private static partial Regex ComentarioDeLinha();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex ComentarioDeBloco();
}
