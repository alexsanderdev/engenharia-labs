using F4M07.Patterns.Pagamentos;
using F4M07.Patterns.Pagamentos.PagaFacil;
using Microsoft.Extensions.DependencyInjection;

namespace F4M07.Patterns.Tests;

/// <summary>Passo 3 — ADAPTER: o SDK estranho do PagaFácil atrás da porta <see cref="IGatewayDePagamento"/>.</summary>
public sealed class AdapterPagamentoTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly Guid PedidoId = Guid.Parse("6f1c2a9e-0d1b-4c7e-9a52-3b8f0e4d7a10");

    private PagaFacilChargeRequest? _ultimaRequisicao;

    private PagaFacilAdapter CriarAdapter(Func<PagaFacilChargeRequest, PagaFacilChargeResponse> resposta) =>
        new(new PagaFacilClient(req =>
        {
            _ultimaRequisicao = req;
            return resposta(req);
        }));

    [Fact]
    public async Task Cobrar_TraduzValorParaCentavosEmTextoMoedaNumericaEReferenciaDoPedido()
    {
        var adapter = CriarAdapter(_ => new PagaFacilChargeResponse { Status = 0, AuthCode = "A1" });

        await adapter.CobrarAsync(new Cobranca(PedidoId, 1_234.50m, "tok_visa"), Ct);

        _ultimaRequisicao.ShouldNotBeNull();
        _ultimaRequisicao.AmountInCents.ShouldBe("123450");
        _ultimaRequisicao.CurrencyCode.ShouldBe("986");
        _ultimaRequisicao.MerchantReference.ShouldBe("6f1c2a9e0d1b4c7e9a523b8f0e4d7a10");
        _ultimaRequisicao.CardHash.ShouldBe("tok_visa");
    }

    [Fact]
    public async Task Cobrar_Status0_DevolveAprovadaComCodigoDeAutorizacao()
    {
        var adapter = CriarAdapter(_ => new PagaFacilChargeResponse { Status = 0, AuthCode = "AUT-777" });

        var resultado = await adapter.CobrarAsync(new Cobranca(PedidoId, 19.99m, "tok"), Ct);

        resultado.ShouldBe(new ResultadoDaCobranca.Aprovada("AUT-777"));
    }

    [Theory]
    [InlineData("51", MotivoDeRecusa.SaldoInsuficiente)]
    [InlineData("14", MotivoDeRecusa.CartaoInvalido)]
    [InlineData("59", MotivoDeRecusa.SuspeitaDeFraude)]
    [InlineData("05", MotivoDeRecusa.Outro)]
    [InlineData(null, MotivoDeRecusa.Outro)]
    public async Task Cobrar_Status1_TraduzCodigoDaAdquirenteParaMotivoDoDominio(string? codigo, MotivoDeRecusa motivo)
    {
        var adapter = CriarAdapter(_ => new PagaFacilChargeResponse { Status = 1, DeclineCode = codigo });

        var resultado = await adapter.CobrarAsync(new Cobranca(PedidoId, 19.99m, "tok"), Ct);

        resultado.ShouldBe(new ResultadoDaCobranca.Recusada(motivo));
    }

    [Fact]
    public async Task Cobrar_StatusDeErroOuTimeout_ViraGatewayIndisponivelSemVazarExcecaoDoSdk()
    {
        var erro = CriarAdapter(_ => new PagaFacilChargeResponse { Status = 9 });
        var timeout = CriarAdapter(_ => throw new PagaFacilTimeoutException("timeout após 30s"));

        (await erro.CobrarAsync(new Cobranca(PedidoId, 10m, "tok"), Ct)).ShouldBeOfType<ResultadoDaCobranca.GatewayIndisponivel>();
        var resultado = await timeout.CobrarAsync(new Cobranca(PedidoId, 10m, "tok"), Ct);
        resultado.ShouldBeOfType<ResultadoDaCobranca.GatewayIndisponivel>().Detalhe.ShouldContain("timeout");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Cobrar_ValorNaoPositivo_LancaArgumentOutOfRangeSemChamarOGateway(decimal valor)
    {
        var adapter = CriarAdapter(_ => throw new InvalidOperationException("não deveria chamar o gateway"));

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => adapter.CobrarAsync(new Cobranca(PedidoId, valor, "tok"), Ct));
    }

    [Fact]
    public async Task Cobrar_ValorComMaisDeDuasCasas_LancaArgumentException()
    {
        var adapter = CriarAdapter(_ => new PagaFacilChargeResponse { Status = 0 });

        await Should.ThrowAsync<ArgumentException>(() => adapter.CobrarAsync(new Cobranca(PedidoId, 10.005m, "tok"), Ct));
    }

    [Fact]
    public async Task ViaDI_PortaDoDominio_ResolveParaOAdapterUsandoOClienteRegistrado()
    {
        using var provider = Composicao.CriarProvider(services =>
            services.AddSingleton(new PagaFacilClient(_ => new PagaFacilChargeResponse { Status = 1, DeclineCode = "51" })));

        var gateway = provider.GetRequiredService<IGatewayDePagamento>();

        gateway.ShouldBeOfType<PagaFacilAdapter>();
        (await gateway.CobrarAsync(new Cobranca(PedidoId, 50m, "tok"), Ct))
            .ShouldBe(new ResultadoDaCobranca.Recusada(MotivoDeRecusa.SaldoInsuficiente));
    }
}
