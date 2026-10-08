using System.Collections.Concurrent;

namespace F3M06.Dapper.Leitura;

/// <summary>
/// Lê consultas guardadas em arquivos <c>Sql/*.sql</c> (copiados para o output pelo .csproj).
/// Consultas grandes (relatórios) ficam mais legíveis e revisáveis num arquivo .sql do que numa string C#.
/// O conteúdo é lido uma vez e mantido em cache.
/// </summary>
public static class SqlArquivos
{
    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static string Ler(string nomeDoArquivo) =>
        Cache.GetOrAdd(nomeDoArquivo, static nome =>
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "Sql", nome);
            return File.Exists(caminho)
                ? File.ReadAllText(caminho)
                : throw new FileNotFoundException($"Arquivo SQL não encontrado no output: {caminho}. Confira o <None Include=\"Sql\\**\\*.sql\"> no .csproj.", caminho);
        });
}
