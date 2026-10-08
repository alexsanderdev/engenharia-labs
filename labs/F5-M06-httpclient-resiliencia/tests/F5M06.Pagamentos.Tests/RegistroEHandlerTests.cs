using F5M06.Pagamentos.Gateway;
using F5M06.Pagamentos.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;
using static F5M06.Pagamentos.Tests.Infra.GatewayFake;

namespace F5M06.Pagamentos.Tests;

/// <summary>Passos 2 e 3 — IHttpClientFactory, typed client e DelegatingHandler.</summary>
public sealed class RegistroEHandlerTests
{
    [Fact]
    public void AddGatewayPagamento_RegistraClienteNomeadoComBaseAddressDasOptionsESemTimeoutDoHttpClient()
    {
        using var c = new Cenario();

        var http = c.Servicos.GetRequiredService<IHttpClientFactory>().CreateClient(DependencyInjection.NomeCliente);
        http.BaseAddress.ShouldBe(c.Gateway.Url);
        http.Timeout.ShouldBe(Timeout.InfiniteTimeSpan, "quem controla o tempo é o pipeline (timeout total e por tentativa)");

        var cliente = c.Cliente;
        cliente.ShouldBeOfType<GatewayPagamentoClient>();
        cliente.ShouldNotBeSameAs(c.Cliente, "typed client é transiente; o que o factory reaproveita é o handler (e as conexões)");
    }

    [Fact]
    public async Task Cobrar_HandlerEnviaApiKeyDasOptionsECorrelationIdPorChamada()
    {
        using var c = new Cenario();
        c.Gateway.Sempre(PostCobranca().WithHeader(CorrelacaoEApiKeyHandler.CabecalhoApiKey, c.Gateway.ApiKey), Aprovada());

        (await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "chave-1")).ShouldBeOfType<ResultadoCobranca.Aprovada>();
        (await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "chave-2")).ShouldBeOfType<ResultadoCobranca.Aprovada>();

        var requisicoes = await c.Gateway.RequisicoesAsync(RotaCobrancas, 2);
        requisicoes.Count.ShouldBe(2);
        requisicoes.ShouldAllBe(r => Header(r, CorrelacaoEApiKeyHandler.CabecalhoApiKey) == c.Gateway.ApiKey);
        var correlacoes = requisicoes.Select(r => Header(r, CorrelacaoEApiKeyHandler.CabecalhoCorrelacao)).ToList();
        correlacoes.ShouldAllBe(id => !string.IsNullOrWhiteSpace(id));
        correlacoes.Distinct().Count().ShouldBe(2, "cada chamada lógica tem o seu correlation id");
    }

    [Fact]
    public async Task Cobrar_ApiKeyInvalida_RetornaRejeitadaNaoAutorizadoSemRetentar()
    {
        using var c = new Cenario(o => o.ApiKey = "chave-de-teste-errada");
        c.Gateway.Servidor.Given(PostCobranca().WithHeader(CorrelacaoEApiKeyHandler.CabecalhoApiKey, c.Gateway.ApiKey))
            .AtPriority(1).RespondWith(Aprovada());
        c.Gateway.Servidor.Given(PostCobranca()).AtPriority(10).RespondWith(Status(401));

        var resultado = await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "chave-1");

        resultado.ShouldBe(new ResultadoCobranca.Rejeitada(MotivoRejeicao.NaoAutorizado));
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 1)).Count.ShouldBe(1, "401 não é transitório: repetir não conserta a chave");
    }
}
