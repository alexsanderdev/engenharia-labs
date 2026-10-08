using Microsoft.Extensions.Configuration;

namespace F1M08.Config;

/// <summary>
/// Monta a configuração do OrderFlow na mesma ordem que o Host.CreateApplicationBuilder usa,
/// mas de forma explícita (e testável). Regra de ouro: o provider adicionado POR ÚLTIMO vence.
/// </summary>
public static class ConfigurationFactory
{
    /// <summary>
    /// Ordem (da menor para a maior prioridade):
    /// 1. "appsettings.json" em <paramref name="basePath"/> (obrigatório);
    /// 2. "appsettings.{environmentName}.json" (opcional: se não existir, não falha);
    ///    (no host real, os user-secrets entram aqui, só em Development)
    /// 3. variáveis de ambiente que começam com <paramref name="envVarPrefix"/>
    ///    (o prefixo é removido e "__" vira ":", ex.: ORDERFLOW_Catalog__MaxPageSize → Catalog:MaxPageSize);
    /// 4. <paramref name="overrides"/> em memória (se informado) — prioridade máxima, útil em testes.
    /// Os arquivos JSON NÃO usam reloadOnChange (testes determinísticos).
    /// </summary>
    public static IConfigurationRoot Build(
        string basePath,
        string environmentName,
        string envVarPrefix,
        IEnumerable<KeyValuePair<string, string?>>? overrides = null)
    {
        throw new NotImplementedException(
            "TODO: use new ConfigurationBuilder() com SetBasePath, AddJsonFile (2x), AddEnvironmentVariables(prefix) e AddInMemoryCollection, NESSA ordem.");
    }
}
