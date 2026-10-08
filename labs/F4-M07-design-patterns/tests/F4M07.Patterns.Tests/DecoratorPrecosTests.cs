using F4M07.Patterns.Precos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace F4M07.Patterns.Tests;

/// <summary>Passo 2 — DECORATOR: cache e log em volta do serviço de preços, montados pelo DI.</summary>
public sealed class DecoratorPrecosTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly FakeTimeProvider _tempo = new(new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero));

    private ServicoDePrecosComCache CriarCache(ServicoDePrecosDoCatalogo catalogo, TimeSpan? ttl = null) =>
        new(catalogo, _tempo, Options.Create(new OpcoesDoCacheDePrecos { Ttl = ttl ?? TimeSpan.FromMinutes(5) }));

    [Fact]
    public async Task Cache_SegundaConsultaDoMesmoSku_NaoVaiAoCatalogoEIgnoraMaiusculas()
    {
        var catalogo = new ServicoDePrecosDoCatalogo();
        var precos = CriarCache(catalogo);

        (await precos.ObterPrecoAsync("SKU-CAFE", Ct)).ShouldBe(39.90m);
        (await precos.ObterPrecoAsync("sku-cafe", Ct)).ShouldBe(39.90m);

        catalogo.Consultas.ShouldBe(1);
    }

    [Fact]
    public async Task Cache_DepoisDoTtl_BuscaDeNovoEEnxergaPrecoNovo()
    {
        var catalogo = new ServicoDePrecosDoCatalogo();
        var precos = CriarCache(catalogo, TimeSpan.FromMinutes(5));
        await precos.ObterPrecoAsync("SKU-CAFE", Ct);
        catalogo.AlterarPreco("SKU-CAFE", 44.90m);

        _tempo.Advance(TimeSpan.FromMinutes(4));
        (await precos.ObterPrecoAsync("SKU-CAFE", Ct)).ShouldBe(39.90m, "ainda dentro do TTL: preço do cache");

        _tempo.Advance(TimeSpan.FromMinutes(1));
        (await precos.ObterPrecoAsync("SKU-CAFE", Ct)).ShouldBe(44.90m, "TTL venceu: busca de novo");
        catalogo.Consultas.ShouldBe(2);
    }

    [Fact]
    public async Task Cache_SkuInexistente_NaoGuardaNull()
    {
        var catalogo = new ServicoDePrecosDoCatalogo();
        var precos = CriarCache(catalogo);

        (await precos.ObterPrecoAsync("SKU-NOVO", Ct)).ShouldBeNull();
        catalogo.AlterarPreco("SKU-NOVO", 10m); // produto cadastrado logo depois
        (await precos.ObterPrecoAsync("SKU-NOVO", Ct)).ShouldBe(10m);
    }

    [Fact]
    public async Task Log_DelegaELogaInformationComSkuEDuracao_OuWarningQuandoNaoAcha()
    {
        var logger = new FakeLogger<ServicoDePrecosComLog>();
        var precos = new ServicoDePrecosComLog(new ServicoDePrecosDoCatalogo(), logger, _tempo);

        (await precos.ObterPrecoAsync("SKU-CANECA", Ct)).ShouldBe(59.90m);
        (await precos.ObterPrecoAsync("SKU-X", Ct)).ShouldBeNull();

        var registros = logger.Collector.GetSnapshot();
        registros.Count.ShouldBe(2);
        registros[0].Level.ShouldBe(LogLevel.Information);
        registros[0].Id.Id.ShouldBe(4701);
        registros[0].GetStructuredStateValue("Sku").ShouldBe("SKU-CANECA");
        registros[0].GetStructuredStateValue("DuracaoMs").ShouldNotBeNull();
        registros[1].Level.ShouldBe(LogLevel.Warning);
        registros[1].Id.Id.ShouldBe(4702);
    }

    [Fact]
    public void Decorar_SemRegistroPrevio_LancaInvalidOperation()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() => services.Decorar<IServicoDePrecos, ServicoDePrecosComCache>());
    }

    [Fact]
    public void Decorar_MantemOLifetimeDoRegistroOriginal()
    {
        var services = new ServiceCollection();
        services.AddScoped<IServicoDePrecos, ServicoDePrecosDoCatalogo>();

        services.Decorar<IServicoDePrecos, ServicoDePrecosComLog>();

        var descritor = services.Single(d => d.ServiceType == typeof(IServicoDePrecos));
        descritor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task Decorar_ComRegistroPorTipo_CriaOInternoEInjetaAsDemaisDependenciasDoContainer()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<ServicoDePrecosComLog>>(NullLogger<ServicoDePrecosComLog>.Instance);
        services.AddSingleton<TimeProvider>(_tempo);
        services.AddSingleton<IServicoDePrecos, ServicoDePrecosDoCatalogo>();

        services.Decorar<IServicoDePrecos, ServicoDePrecosComLog>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        var precos = provider.GetRequiredService<IServicoDePrecos>();
        precos.ShouldBeOfType<ServicoDePrecosComLog>();
        (await precos.ObterPrecoAsync("SKU-MOEDOR", Ct)).ShouldBe(349.00m);
    }

    [Fact]
    public async Task ViaDI_LogPorForaDoCache_TodaChamadaEhLogadaMasOCatalogoEhConsultadoUmaVez()
    {
        using var provider = Composicao.CriarProvider(tempo: _tempo);
        var precos = provider.GetRequiredService<IServicoDePrecos>();
        var catalogo = provider.GetRequiredService<ServicoDePrecosDoCatalogo>();
        var logs = provider.GetFakeLogCollector();

        precos.ShouldBeOfType<ServicoDePrecosComLog>("o decorator mais externo é o de log");
        await precos.ObterPrecoAsync("SKU-CAFE", Ct);
        await precos.ObterPrecoAsync("SKU-CAFE", Ct);
        await precos.ObterPrecoAsync("SKU-CAFE", Ct);

        catalogo.Consultas.ShouldBe(1, "cache no meio: só a primeira chamada chega ao catálogo");
        logs.GetSnapshot().Count(r => r.Id.Id == 4701).ShouldBe(3, "log por fora: as três chamadas são registradas");
    }
}
