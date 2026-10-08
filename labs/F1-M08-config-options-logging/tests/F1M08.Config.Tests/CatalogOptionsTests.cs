using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace F1M08.Config.Tests;

/// <summary>Passos 2 e 3: Options pattern, validação e IOptionsMonitor.</summary>
public class CatalogOptionsTests
{
    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["Catalog:StoreName"] = "OrderFlow",
        ["Catalog:MaxPageSize"] = "100",
        ["Catalog:DefaultPageSize"] = "25",
        ["Catalog:MaxPriceIncreasePercent"] = "20",
        ["Catalog:PricingApiKey"] = "chave-de-teste",
    };

    private static ServiceProvider BuildProvider(IConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCatalog(config);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings) =>
        BuildProvider(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

    [Fact]
    public void AddCatalog_ConfiguracaoValida_FazBindDeTodosOsCampos()
    {
        using var sp = BuildProvider(ValidSettings());

        var options = sp.GetRequiredService<IOptions<CatalogOptions>>().Value;

        options.StoreName.ShouldBe("OrderFlow");
        options.MaxPageSize.ShouldBe(100);
        options.DefaultPageSize.ShouldBe(25);
        options.MaxPriceIncreasePercent.ShouldBe(20m);
        options.PricingApiKey.ShouldBe("chave-de-teste");
    }

    [Fact]
    public void AddCatalog_SemStoreName_LancaOptionsValidationException()
    {
        var settings = ValidSettings();
        settings.Remove("Catalog:StoreName");
        using var sp = BuildProvider(settings);

        var ex = Should.Throw<OptionsValidationException>(() => sp.GetRequiredService<IOptions<CatalogOptions>>().Value);

        ex.Failures.ShouldContain(f => f.Contains("StoreName"));
    }

    [Fact]
    public void AddCatalog_MaxPageSizeForaDoIntervalo_LancaOptionsValidationException()
    {
        var settings = ValidSettings();
        settings["Catalog:MaxPageSize"] = "500";
        using var sp = BuildProvider(settings);

        var ex = Should.Throw<OptionsValidationException>(() => sp.GetRequiredService<IOptions<CatalogOptions>>().Value);

        ex.Failures.ShouldContain(f => f.Contains("MaxPageSize"));
    }

    [Fact]
    public void AddCatalog_DefaultMaiorQueMax_FalhaComAMensagemDaRegraCustomizada()
    {
        var settings = ValidSettings();
        settings["Catalog:MaxPageSize"] = "10";
        settings["Catalog:DefaultPageSize"] = "30";
        using var sp = BuildProvider(settings);

        var ex = Should.Throw<OptionsValidationException>(() => sp.GetRequiredService<IOptions<CatalogOptions>>().Value);

        ex.Failures.ShouldContain(CatalogServiceCollectionExtensions.PageSizeRuleMessage);
    }

    [Fact]
    public async Task Host_ComOptionsInvalidas_FalhaNoStartAsync_ValidateOnStart()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        var settings = ValidSettings();
        settings.Remove("Catalog:PricingApiKey"); // segredo esquecido no ambiente
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddCatalog(builder.Configuration);
        using var host = builder.Build();

        // Sem ValidateOnStart, o host subiria "saudável" e o erro só apareceria no primeiro uso.
        await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void OptionsMonitor_AposReloadDaConfiguracao_EntregaNovoValorEDisparaOnChange()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(ValidSettings()).Build();
        using var sp = BuildProvider(config);
        var monitor = sp.GetRequiredService<IOptionsMonitor<CatalogOptions>>();
        monitor.CurrentValue.MaxPageSize.ShouldBe(100);
        int? notified = null;
        using var _ = monitor.OnChange(o => notified = o.MaxPageSize);

        config["Catalog:MaxPageSize"] = "150";
        config.Reload(); // o mesmo sinal que um appsettings.json com reloadOnChange dispara

        monitor.CurrentValue.MaxPageSize.ShouldBe(150);
        notified.ShouldBe(150);
    }

    [Fact]
    public void OptionsSnapshot_EOptions_DiferencaDeTempoDeVida()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(ValidSettings()).Build();
        using var sp = BuildProvider(config);
        var options = sp.GetRequiredService<IOptions<CatalogOptions>>().Value;

        config["Catalog:StoreName"] = "Nova Loja";
        config.Reload();

        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<CatalogOptions>>().Value.StoreName.ShouldBe("Nova Loja");
        sp.GetRequiredService<IOptions<CatalogOptions>>().Value.StoreName.ShouldBe("OrderFlow"); // IOptions é singleton e não recarrega
        options.StoreName.ShouldBe("OrderFlow");
    }
}
