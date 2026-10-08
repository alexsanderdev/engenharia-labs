using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace F1M06.Hosting.Tests;

public class ComposicaoTests
{
    private static ServiceProvider CriarProvider(
        Dictionary<string, string?>? configuracao = null,
        Action<IServiceCollection>? extras = null)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(configuracao ?? []).Build();
        var services = new ServiceCollection();
        services.AddOrderFlow(config);
        extras?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void RepositorioDeProdutos_EhSingleton_MesmaInstanciaEmEscoposDiferentes()
    {
        using var provider = CriarProvider();
        using var escopo1 = provider.CreateScope();
        using var escopo2 = provider.CreateScope();

        escopo1.ServiceProvider.GetRequiredService<IRepositorioProdutos>()
            .ShouldBeSameAs(escopo2.ServiceProvider.GetRequiredService<IRepositorioProdutos>());
    }

    [Fact]
    public void ContextoDaOperacao_EhScoped_IgualNoEscopoDiferenteEntreEscopos()
    {
        using var provider = CriarProvider();
        using var escopo1 = provider.CreateScope();
        using var escopo2 = provider.CreateScope();

        var a = escopo1.ServiceProvider.GetRequiredService<IContextoDaOperacao>();
        var b = escopo1.ServiceProvider.GetRequiredService<IContextoDaOperacao>();
        var c = escopo2.ServiceProvider.GetRequiredService<IContextoDaOperacao>();

        a.ShouldBeSameAs(b);
        a.Id.ShouldNotBe(c.Id);
    }

    [Fact]
    public void ContextoDaOperacao_ResolvidoNaRaiz_ComValidateScopes_Lanca()
    {
        using var provider = CriarProvider();

        // Resolver Scoped direto do provider raiz faria ele viver para sempre: ValidateScopes impede.
        Should.Throw<InvalidOperationException>(() => provider.GetRequiredService<IContextoDaOperacao>());
    }

    [Fact]
    public void ServicoDePrecos_EhDecoradoComCache()
    {
        using var provider = CriarProvider();
        using var escopo = provider.CreateScope();

        escopo.ServiceProvider.GetRequiredService<IServicoDePrecos>().ShouldBeOfType<ServicoDePrecosComCache>();
    }

    [Fact]
    public void ServicoDePrecos_CacheValePorEscopo()
    {
        using var provider = CriarProvider();
        var repositorio = provider.GetRequiredService<IRepositorioProdutos>();

        using (var escopo = provider.CreateScope())
        {
            var precos = escopo.ServiceProvider.GetRequiredService<IServicoDePrecos>();
            precos.ObterPreco("SKU-1").ShouldBe(100m);
            precos.ObterPreco("sku-1").ShouldBe(100m);
            precos.ObterPreco("SKU-2").ShouldBe(250m);
        }

        repositorio.Consultas.ShouldBe(2, "a segunda consulta de SKU-1 deve vir do cache");

        using (var escopo = provider.CreateScope())
            escopo.ServiceProvider.GetRequiredService<IServicoDePrecos>().ObterPreco("SKU-1");

        repositorio.Consultas.ShouldBe(3, "escopo novo = cache novo");
    }

    [Fact]
    public void Frete_KeyedServices_ResolvemImplementacaoPelaChave()
    {
        using var provider = CriarProvider();

        provider.GetRequiredKeyedService<ICalculadoraDeFrete>(ChavesFrete.Padrao).ShouldBeOfType<FretePadrao>();
        provider.GetRequiredKeyedService<ICalculadoraDeFrete>(ChavesFrete.Expresso).Calcular(500m).ShouldBe(45m);
    }

    [Fact]
    public void Frete_ChaveInexistente_Lanca()
    {
        using var provider = CriarProvider();

        Should.Throw<InvalidOperationException>(() => provider.GetRequiredKeyedService<ICalculadoraDeFrete>("drone"));
    }

    [Fact]
    public void Checkout_RecebeOFretePadraoViaFromKeyedServices()
    {
        using var provider = CriarProvider();
        using var escopo = provider.CreateScope();

        var checkout = escopo.ServiceProvider.GetRequiredService<ServicoDeCheckout>();

        checkout.TotalComFrete(100m).ShouldBe(120m);
        checkout.TotalComFrete(200m).ShouldBe(200m);
    }

    [Fact]
    public void GeradorDeCodigo_CriadoPorFactory_UsaOpcoesETimeProvider()
    {
        var relogio = new FakeTimeProvider(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));
        using var provider = CriarProvider(
            new() { ["Pedidos:Prefixo"] = "OFX" },
            s => s.AddSingleton<TimeProvider>(relogio));

        var gerador = provider.GetRequiredService<IGeradorDeCodigoPedido>();

        gerador.Proximo().ShouldBe("OFX-2026-000001");
        gerador.Proximo().ShouldBe("OFX-2026-000002");
        provider.GetRequiredService<IGeradorDeCodigoPedido>().ShouldBeSameAs(gerador);
    }

    [Fact]
    public void OpcoesPedidos_SemConfiguracao_UsaValoresPadrao()
    {
        using var provider = CriarProvider();

        var opcoes = provider.GetRequiredService<IOptions<OpcoesPedidos>>().Value;

        opcoes.Prefixo.ShouldBe("PED");
        opcoes.IntervaloLimpeza.ShouldBe(TimeSpan.FromMinutes(5));
    }
}
