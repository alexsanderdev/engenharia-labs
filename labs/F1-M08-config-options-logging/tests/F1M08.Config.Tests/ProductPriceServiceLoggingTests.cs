using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace F1M08.Config.Tests;

/// <summary>Passo 4: logging estruturado com [LoggerMessage], verificado com FakeLogger.</summary>
public sealed class ProductPriceServiceLoggingTests : IDisposable
{
    private const string Secret = "sk-super-secreta-123";
    private readonly IConfigurationRoot _config;
    private readonly ServiceProvider _sp;
    private readonly FakeLogCollector _logs;
    private readonly ProductPriceService _service;

    public ProductPriceServiceLoggingTests()
    {
        _config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Catalog:StoreName"] = "OrderFlow",
            ["Catalog:MaxPriceIncreasePercent"] = "10",
            ["Catalog:PricingApiKey"] = Secret,
        }).Build();

        var services = new ServiceCollection();
        services.AddFakeLogging(); // ILogger<T> passa a gravar num FakeLogCollector
        services.AddCatalog(_config);
        _sp = services.BuildServiceProvider();

        _logs = _sp.GetFakeLogCollector();
        _service = _sp.GetRequiredService<ProductPriceService>();
    }

    public void Dispose() => _sp.Dispose();

    [Fact]
    public void ChangePrice_DentroDoLimite_RetornaTrueELogaInformation1001ComCamposEstruturados()
    {
        var productId = Guid.NewGuid();

        _service.ChangePrice(productId, 100m, 110m).ShouldBeTrue();

        var record = _logs.LatestRecord;
        record.Level.ShouldBe(LogLevel.Information);
        record.Id.Id.ShouldBe(1001);
        record.Category.ShouldBe(typeof(ProductPriceService).FullName);
        record.GetStructuredStateValue("ProductId").ShouldBe(productId.ToString());
        record.Message.ShouldContain(productId.ToString());
    }

    [Fact]
    public void ChangePrice_AcimaDoLimite_RetornaFalseELogaWarning1002()
    {
        _service.ChangePrice(Guid.NewGuid(), 100m, 110.01m).ShouldBeFalse();

        var record = _logs.LatestRecord;
        record.Level.ShouldBe(LogLevel.Warning);
        record.Id.Id.ShouldBe(1002);
        record.GetStructuredStateValue("LimitPercent").ShouldNotBeNull();
    }

    [Fact]
    public void ChangePrice_ReducaoDePreco_SempreAceita()
    {
        _service.ChangePrice(Guid.NewGuid(), 100m, 1m).ShouldBeTrue();
    }

    [Fact]
    public void ChangePrice_LimiteAlteradoEmRuntime_ValeSemReiniciar()
    {
        var id = Guid.NewGuid();
        _service.ChangePrice(id, 100m, 150m).ShouldBeFalse();

        _config["Catalog:MaxPriceIncreasePercent"] = "60";
        _config.Reload();

        _service.ChangePrice(id, 100m, 150m).ShouldBeTrue();
        _logs.GetSnapshot().Select(r => r.Id.Id).ShouldBe([1002, 1001]);
    }

    [Fact]
    public void LogStartupSummary_LogaConfiguracaoSemVazarOSegredo()
    {
        _service.LogStartupSummary();

        var record = _logs.LatestRecord;
        record.Id.Id.ShouldBe(1000);
        record.GetStructuredStateValue("StoreName").ShouldBe("OrderFlow");
        _logs.GetSnapshot().ShouldAllBe(r => !r.Message.Contains(Secret));
        _logs.GetSnapshot().SelectMany(r => r.StructuredState ?? []).ShouldAllBe(kv => kv.Value != Secret);
    }
}
