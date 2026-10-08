using F4M07.Patterns.Frete;
using Microsoft.Extensions.DependencyInjection;

namespace F4M07.Patterns.Tests;

/// <summary>Passo 1 — STRATEGY: uma classe por modalidade de frete, escolhida pela chave, sem switch.</summary>
public sealed class StrategyFreteTests
{
    private static readonly PedidoParaFrete PedidoLeveSp = new(Subtotal: 120m, PesoKg: 2.3m, Uf: "SP");

    [Theory]
    [InlineData(120, 2.3, "SP", 21, 5)]   // 15 + 2 × 3 kg iniciados
    [InlineData(120, 0, "BA", 15, 8)]     // peso zero: só a base; fora de SP: 8 dias
    [InlineData(300, 10, "SP", 0, 5)]     // a partir de R$ 300: grátis
    public void FreteEconomico_Calcular_CobraPorKgIniciadoEZeraAPartirDe300(
        decimal subtotal, decimal peso, string uf, decimal valorEsperado, int prazoEsperado)
    {
        var cotacao = new FreteEconomico().Calcular(new PedidoParaFrete(subtotal, peso, uf));

        cotacao.ShouldBe(new CotacaoDeFrete(ModalidadeFrete.Economico, valorEsperado, prazoEsperado));
    }

    [Fact]
    public void FreteExpresso_Calcular_NuncaEhGratisEPrazoCurtoEmSp()
    {
        new FreteExpresso().Calcular(new PedidoParaFrete(1_000m, 2.3m, "sp"))
            .ShouldBe(new CotacaoDeFrete(ModalidadeFrete.Expresso, 42m, 1));
        new FreteExpresso().Calcular(new PedidoParaFrete(50m, 1m, "RS"))
            .ShouldBe(new CotacaoDeFrete(ModalidadeFrete.Expresso, 34m, 3));
    }

    [Fact]
    public void RetiradaNaLoja_Calcular_EhGratisEmUmDia()
    {
        new RetiradaNaLoja().Calcular(new PedidoParaFrete(10m, 50m, "AM"))
            .ShouldBe(new CotacaoDeFrete(ModalidadeFrete.Retirada, 0m, 1));
    }

    [Theory]
    [MemberData(nameof(TodasAsEstrategias))]
    public void TodaEstrategia_CumpreOContrato_ValorEPrazoNaoNegativos(ICalculadoraDeFrete estrategia)
    {
        foreach (var pedido in new[] { PedidoLeveSp, new PedidoParaFrete(0m, 0m, "AC"), new PedidoParaFrete(5_000m, 80m, "SP") })
        {
            var cotacao = estrategia.Calcular(pedido);
            cotacao.Modalidade.ShouldBe(estrategia.Modalidade);
            cotacao.Valor.ShouldBeGreaterThanOrEqualTo(0m);
            cotacao.PrazoEmDiasUteis.ShouldBeGreaterThanOrEqualTo(0);
        }
    }

    public static TheoryData<ICalculadoraDeFrete> TodasAsEstrategias() =>
        [new FreteEconomico(), new FreteExpresso(), new RetiradaNaLoja()];

    [Fact]
    public void Cotador_Cotar_EscolheAEstrategiaPelaModalidadeSemDiferenciarMaiusculas()
    {
        var cotador = new CotadorDeFrete([new FreteEconomico(), new FreteExpresso(), new RetiradaNaLoja()]);

        cotador.Cotar("EXPRESSO", PedidoLeveSp).Valor.ShouldBe(42m);
        cotador.Cotar(ModalidadeFrete.Economico, PedidoLeveSp).Valor.ShouldBe(21m);
    }

    [Fact]
    public void Cotador_ModalidadeDesconhecida_LancaExcecaoListandoAsDisponiveis()
    {
        var cotador = new CotadorDeFrete([new FreteEconomico(), new FreteExpresso()]);

        var ex = Should.Throw<ModalidadeDeFreteDesconhecidaException>(() => cotador.Cotar("drone", PedidoLeveSp));

        ex.Modalidade.ShouldBe("drone");
        ex.Message.ShouldContain("economico");
        ex.Message.ShouldContain("expresso");
    }

    [Fact]
    public void Cotador_ModalidadeDuplicada_EhErroDeComposicao()
    {
        Should.Throw<InvalidOperationException>(() => new CotadorDeFrete([new FreteEconomico(), new FreteEconomico()]));
    }

    [Fact]
    public void Cotador_CotarTodas_OrdenaDaMaisBarataParaAMaisCaraEDesempataPeloPrazo()
    {
        var cotador = new CotadorDeFrete([new FreteExpresso(), new FreteEconomico(), new RetiradaNaLoja()]);

        var cotacoes = cotador.CotarTodas(new PedidoParaFrete(400m, 1m, "SP")); // econômico grátis (5 dias) e retirada grátis (1 dia)

        cotacoes.Select(c => c.Modalidade).ShouldBe([ModalidadeFrete.Retirada, ModalidadeFrete.Economico, ModalidadeFrete.Expresso]);
    }

    [Fact]
    public void ViaDI_KeyedServices_CadaModalidadeResolvidaPelaSuaChave()
    {
        using var provider = Composicao.CriarProvider();

        provider.GetRequiredKeyedService<ICalculadoraDeFrete>(ModalidadeFrete.Expresso).ShouldBeOfType<FreteExpresso>();
        provider.GetRequiredKeyedService<ICalculadoraDeFrete>(ModalidadeFrete.Retirada).ShouldBeOfType<RetiradaNaLoja>();
        provider.GetRequiredService<ICotadorDeFrete>().CotarTodas(PedidoLeveSp).Count.ShouldBe(3);
    }

    [Fact]
    public void ViaDI_ModalidadeNovaRegistrada_EntraNoCotadorSemAlterarCodigoExistente()
    {
        using var provider = Composicao.CriarProvider(services =>
            services.AddKeyedSingleton<ICalculadoraDeFrete, FreteDeMotoboy>(FreteDeMotoboy.Chave));

        var cotador = provider.GetRequiredService<ICotadorDeFrete>();

        cotador.Cotar("motoboy", PedidoLeveSp).Valor.ShouldBe(12m);
        cotador.CotarTodas(PedidoLeveSp).Count.ShouldBe(4);
    }

    private sealed class FreteDeMotoboy : ICalculadoraDeFrete
    {
        public const string Chave = "motoboy";

        public string Modalidade => Chave;

        public CotacaoDeFrete Calcular(PedidoParaFrete pedido) => new(Chave, 12m, 0);
    }
}
