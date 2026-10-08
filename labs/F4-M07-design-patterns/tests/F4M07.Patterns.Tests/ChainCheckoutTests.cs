using F4M07.Patterns.Checkout;
using Microsoft.Extensions.DependencyInjection;

namespace F4M07.Patterns.Tests;

/// <summary>Passo 6 — CHAIN OF RESPONSIBILITY: validações de checkout como uma corrente de elos independentes.</summary>
public sealed class ChainCheckoutTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly CatalogoEmMemoria _catalogo = new CatalogoEmMemoria()
        .Com("SKU-CAFE", estoque: 5)
        .Com("SKU-CANECA", estoque: 2)
        .Com("SKU-ANTIGO", ativo: false);

    private static ContextoDeCheckout Carrinho(params ItemDoCarrinho[] itens) => new(Guid.NewGuid(), itens);

    private PipelineDeValidacaoDoCheckout PipelinePadrao(params IValidacaoDeCheckout[] extras) =>
        new([new CarrinhoNaoVazio(), new QuantidadesPositivas(), new ProdutosAtivos(_catalogo), new EstoqueSuficiente(_catalogo), .. extras]);

    [Fact]
    public async Task Pipeline_CarrinhoValido_PassaPorTodosOsElos()
    {
        var espiao = new Espiao();

        var resultado = await PipelinePadrao(espiao).ValidarAsync(Carrinho(new ItemDoCarrinho("SKU-CAFE", 2), new ItemDoCarrinho("SKU-CANECA", 1)), Ct);

        resultado.ShouldBe(ResultadoDaValidacao.Valido);
        espiao.Chamadas.ShouldBe(1);
    }

    [Fact]
    public async Task Pipeline_SemValidacoes_EhValido()
    {
        (await new PipelineDeValidacaoDoCheckout([]).ValidarAsync(Carrinho(), Ct)).EhValido.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(CarrinhosInvalidos))]
    public async Task Pipeline_PrimeiroEloQueFalha_DefineOCodigo(ItemDoCarrinho[] itens, string codigoEsperado)
    {
        var resultado = await PipelinePadrao().ValidarAsync(Carrinho(itens), Ct);

        resultado.EhValido.ShouldBeFalse();
        resultado.Codigo.ShouldBe(codigoEsperado);
    }

    public static TheoryData<ItemDoCarrinho[], string> CarrinhosInvalidos() => new()
    {
        { [], "carrinho.vazio" },
        { [new("SKU-CAFE", 0)], "item.quantidade_invalida" },
        { [new("SKU-CAFE", 1), new ItemDoCarrinho("SKU-NAO-EXISTE", 1)], "produto.inexistente" },
        { [new("SKU-ANTIGO", 1)], "produto.inativo" },
        { [new("SKU-CANECA", 3)], "estoque.insuficiente" },
        { [new("SKU-CAFE", 3), new("sku-cafe", 3)], "estoque.insuficiente" }, // mesmo SKU em duas linhas: 6 > 5
        { [new("SKU-ANTIGO", -1)], "item.quantidade_invalida" },              // a ordem importa: quantidade antes de produto
    };

    [Fact]
    public async Task Pipeline_EloQueFalha_InterrompeACorrenteEOsSeguintesNaoExecutam()
    {
        var espiao = new Espiao();

        var resultado = await PipelinePadrao(espiao).ValidarAsync(Carrinho(new ItemDoCarrinho("SKU-ANTIGO", 1)), Ct);

        resultado.Codigo.ShouldBe("produto.inativo");
        resultado.Mensagem!.ShouldContain("SKU-ANTIGO");
        espiao.Chamadas.ShouldBe(0);
    }

    [Fact]
    public async Task Pipeline_ExecutaOsElosNaOrdemEmQueForamRegistrados()
    {
        var ordem = new List<string>();
        var pipeline = new PipelineDeValidacaoDoCheckout([new Marcador("A", ordem), new Marcador("B", ordem), new Marcador("C", ordem)]);

        await pipeline.ValidarAsync(Carrinho(), Ct);

        ordem.ShouldBe(["A", "B", "C"]);
    }

    [Fact]
    public async Task Pipeline_TokenCancelado_LancaAntesDeExecutarOsElos()
    {
        var espiao = new Espiao();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await new PipelineDeValidacaoDoCheckout([espiao]).ValidarAsync(Carrinho(), cts.Token));
        espiao.Chamadas.ShouldBe(0);
    }

    [Fact]
    public async Task ViaDI_ValidacoesRegistradasNaOrdemCertaEPipelineResolvido()
    {
        using var provider = Composicao.CriarProvider(services => services.AddSingleton<ICatalogoParaCheckout>(_catalogo));
        using var escopo = provider.CreateScope();

        escopo.ServiceProvider.GetServices<IValidacaoDeCheckout>().Select(v => v.GetType()).ShouldBe(
            [typeof(CarrinhoNaoVazio), typeof(QuantidadesPositivas), typeof(ProdutosAtivos), typeof(EstoqueSuficiente)]);

        var pipeline = escopo.ServiceProvider.GetRequiredService<PipelineDeValidacaoDoCheckout>();
        (await pipeline.ValidarAsync(Carrinho(new ItemDoCarrinho("SKU-CAFE", 1)), Ct)).EhValido.ShouldBeTrue();
        (await pipeline.ValidarAsync(Carrinho(new ItemDoCarrinho("SKU-CANECA", 99)), Ct)).Codigo.ShouldBe("estoque.insuficiente");
    }

    [Fact]
    public async Task ViaDI_ValidacaoNovaRegistradaDepois_EntraNoFimDaCorrenteSemAlterarOPipeline()
    {
        var espiao = new Espiao();
        using var provider = Composicao.CriarProvider(
            antes: services => services.AddSingleton<ICatalogoParaCheckout>(_catalogo),
            depois: services => services.AddSingleton<IValidacaoDeCheckout>(espiao));
        using var escopo = provider.CreateScope();
        var pipeline = escopo.ServiceProvider.GetRequiredService<PipelineDeValidacaoDoCheckout>();

        (await pipeline.ValidarAsync(Carrinho(new ItemDoCarrinho("SKU-CAFE", 1)), Ct)).EhValido.ShouldBeTrue();
        espiao.Chamadas.ShouldBe(1);

        (await pipeline.ValidarAsync(Carrinho(), Ct)).Codigo.ShouldBe("carrinho.vazio");
        espiao.Chamadas.ShouldBe(1, "a corrente parou antes do último elo");
    }

    private sealed class Espiao : IValidacaoDeCheckout
    {
        public int Chamadas { get; private set; }

        public ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct)
        {
            Chamadas++;
            return proxima();
        }
    }

    private sealed class Marcador(string nome, List<string> ordem) : IValidacaoDeCheckout
    {
        public ValueTask<ResultadoDaValidacao> ValidarAsync(ContextoDeCheckout contexto, ProximaValidacao proxima, CancellationToken ct)
        {
            ordem.Add(nome);
            return proxima();
        }
    }
}
