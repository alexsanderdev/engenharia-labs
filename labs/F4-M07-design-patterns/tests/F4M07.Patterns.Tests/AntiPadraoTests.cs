using F4M07.Patterns.Checkout;
using F4M07.Patterns.Frete;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetArchTest.Rules;

namespace F4M07.Patterns.Tests;

/// <summary>
/// Passo 7 — ANTI-PADRÃO: a <see cref="CalculadoraDeTotalDoPedido"/> veio do legado usando um Singleton estático
/// (<c>ConfiguracaoGlobal</c>) e um Service Locator (<c>LocalizadorDeServicos</c>). Troque por DI explícita.
/// </summary>
public sealed class AntiPadraoTests
{
    private const string NamespaceLegado = "F4M07.Patterns.Legado";

    private static readonly ICotadorDeFrete Cotador = new CotadorDeFrete([new FreteEconomico(), new FreteExpresso()]);

    private static CalculadoraDeTotalDoPedido Calculadora(decimal percentual) =>
        new(Cotador, Options.Create(new OpcoesDeCheckout { PercentualDeTaxaDeServico = percentual }));

    [Fact]
    public void Calcular_UsaOCotadorEAsOpcoesRecebidosNoConstrutor()
    {
        var total = Calculadora(0.02m).Calcular(subtotal: 120m, pesoKg: 2.3m, uf: "SP", ModalidadeFrete.Economico);

        total.TaxaDeServico.ShouldBe(2.40m);
        total.Frete.Valor.ShouldBe(21m);
        total.Total.ShouldBe(143.40m);
    }

    [Fact]
    public void Calcular_DuasConfiguracoesLadoALado_NaoInterferemUmaNaOutra()
    {
        // Com um Singleton estático isto é impossível: só existe UMA configuração no processo inteiro.
        var semTaxa = Calculadora(0m);
        var taxaCheia = Calculadora(0.10m);

        semTaxa.Calcular(100m, 1m, "SP", ModalidadeFrete.Expresso).TaxaDeServico.ShouldBe(0m);
        taxaCheia.Calcular(100m, 1m, "SP", ModalidadeFrete.Expresso).TaxaDeServico.ShouldBe(10m);
        semTaxa.Calcular(100m, 1m, "SP", ModalidadeFrete.Expresso).TaxaDeServico.ShouldBe(0m);
    }

    [Fact]
    public void Calcular_TaxaArredondaParaDuasCasasAwayFromZero()
    {
        Calculadora(0.015m).Calcular(33.30m, 0m, "SP", ModalidadeFrete.Expresso).TaxaDeServico.ShouldBe(0.50m); // 0,4995
        Calculadora(0.05m).Calcular(0.10m, 0m, "SP", ModalidadeFrete.Expresso).TaxaDeServico.ShouldBe(0.01m);   // 0,005
    }

    [Fact]
    public void ViaDI_CalculadoraResolvidaComOpcoesConfiguradas()
    {
        using var provider = Composicao.CriarProvider(services =>
            services.Configure<OpcoesDeCheckout>(o => o.PercentualDeTaxaDeServico = 0.03m));
        using var escopo = provider.CreateScope();

        var total = escopo.ServiceProvider.GetRequiredService<CalculadoraDeTotalDoPedido>()
            .Calcular(200m, 1m, "RJ", ModalidadeFrete.Retirada);

        total.TaxaDeServico.ShouldBe(6m);
        total.Total.ShouldBe(206m);
    }

    [Fact]
    public void Arquitetura_NadaForaDoLegadoDependeDoSingletonEstaticoNemDoServiceLocator()
    {
        var resultado = Types.InAssembly(typeof(CalculadoraDeTotalDoPedido).Assembly)
            .That().DoNotResideInNamespace(NamespaceLegado)
            .ShouldNot().HaveDependencyOn(NamespaceLegado)
            .GetResult();

        resultado.IsSuccessful.ShouldBeTrue(
            "tipos que ainda usam o legado: " + string.Join(", ", resultado.FailingTypeNames ?? []));
    }
}
