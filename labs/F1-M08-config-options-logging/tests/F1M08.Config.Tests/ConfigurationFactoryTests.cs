using Microsoft.Extensions.Configuration;

namespace F1M08.Config.Tests;

/// <summary>
/// Passo 1: precedência de providers. Cada teste usa uma pasta temporária própria e um prefixo
/// de variável de ambiente único, para rodar em paralelo sem interferência.
/// </summary>
public sealed class ConfigurationFactoryTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("f1m08-").FullName;
    private readonly string _prefix = $"F1M08_{Guid.NewGuid():N}_";
    private readonly List<string> _envVars = [];

    public ConfigurationFactoryTests()
    {
        File.WriteAllText(Path.Combine(_dir, "appsettings.json"), """
            {
              "Catalog": {
                "StoreName": "OrderFlow Base",
                "MaxPageSize": 50,
                "DefaultPageSize": 20
              }
            }
            """);
    }

    private void WriteEnvironmentFile(string env, string json) =>
        File.WriteAllText(Path.Combine(_dir, $"appsettings.{env}.json"), json);

    private void SetEnv(string key, string value)
    {
        var name = _prefix + key;
        _envVars.Add(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose()
    {
        foreach (var name in _envVars)
            Environment.SetEnvironmentVariable(name, null);
        Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Build_SoAppSettings_LeValoresDoJson()
    {
        var config = ConfigurationFactory.Build(_dir, "Production", _prefix);

        config["Catalog:StoreName"].ShouldBe("OrderFlow Base");
        config["Catalog:MaxPageSize"].ShouldBe("50");
    }

    [Fact]
    public void Build_ArquivoDoAmbienteAusente_NaoFalha()
    {
        Should.NotThrow(() => ConfigurationFactory.Build(_dir, "Staging", _prefix));
    }

    [Fact]
    public void Build_AppSettingsDoAmbiente_SobrescreveSoAsChavesQueDefine()
    {
        WriteEnvironmentFile("Development", """{ "Catalog": { "StoreName": "OrderFlow Dev" } }""");

        var config = ConfigurationFactory.Build(_dir, "Development", _prefix);

        config["Catalog:StoreName"].ShouldBe("OrderFlow Dev");
        config["Catalog:MaxPageSize"].ShouldBe("50"); // merge por chave, não por arquivo
    }

    [Fact]
    public void Build_VariavelDeAmbiente_SobrescreveJsonDoAmbiente()
    {
        WriteEnvironmentFile("Development", """{ "Catalog": { "MaxPageSize": 80 } }""");
        SetEnv("Catalog__MaxPageSize", "120"); // "__" vira ":"

        var config = ConfigurationFactory.Build(_dir, "Development", _prefix);

        config["Catalog:MaxPageSize"].ShouldBe("120");
    }

    [Fact]
    public void Build_VariavelSemOPrefixo_EIgnorada()
    {
        var semPrefixo = $"OUTRO_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(semPrefixo, "x");
        try
        {
            var config = ConfigurationFactory.Build(_dir, "Production", _prefix);

            config.AsEnumerable().ShouldNotContain(kv => kv.Key.Contains(semPrefixo, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.SetEnvironmentVariable(semPrefixo, null);
        }
    }

    [Fact]
    public void Build_InMemory_TemPrioridadeMaxima()
    {
        SetEnv("Catalog__StoreName", "Da variável");

        var config = ConfigurationFactory.Build(_dir, "Production", _prefix,
            [new("Catalog:StoreName", "Do teste")]);

        config["Catalog:StoreName"].ShouldBe("Do teste");
    }
}
