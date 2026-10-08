using System.Net;
using F5M05.Api.Relatorios;
using F5M05.Api.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;
using static F5M05.Api.Tests.Infra.Http;

namespace F5M05.Api.Tests;

/// <summary>
/// Parte (a), Passos 2 e 3 — as políticas aplicadas aos endpoints: 429 + Retry-After + ProblemDetails,
/// partição por cliente e por IP, e políticas independentes por endpoint.
/// </summary>
public sealed class RateLimitTests
{
    [Fact]
    public async Task CriarPedido_AlemDoLimiteDoCliente_Retorna429ComRetryAfterEProblemDetails()
    {
        await using var api = new ApiFactory(l => l.Pedidos.Limite = 2);
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);

        (await ana.PostarPedidoAsync(NovoPedido(), "k1")).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await ana.PostarPedidoAsync(NovoPedido(), "k2")).StatusCode.ShouldBe(HttpStatusCode.Created);
        var rejeitada = await ana.PostarPedidoAsync(NovoPedido(), "k3");

        rejeitada.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        var retryAfter = rejeitada.Headers.RetryAfter.ShouldNotBeNull().Delta.ShouldNotBeNull();
        retryAfter.ShouldBeInRange(TimeSpan.FromSeconds(1), TimeSpan.FromHours(1));
        rejeitada.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await rejeitada.JsonAsync()).GetProperty("status").GetInt32().ShouldBe(429);
        api.Pedidos.Quantidade.ShouldBe(2, "a requisição rejeitada não chega ao handler");
    }

    [Fact]
    public async Task CriarPedido_LimiteEPorCliente_OutroClienteNaoEAfetado()
    {
        await using var api = new ApiFactory(l => l.Pedidos.Limite = 1);
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA, ip: "10.0.0.1");
        using var bruno = api.ClienteComChave(ApiFactory.ChaveClienteB, ip: "10.0.0.1"); // mesmo IP (NAT corporativo)

        (await ana.PostarPedidoAsync(NovoPedido(), "a1")).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await ana.PostarPedidoAsync(NovoPedido(), "a2")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        (await bruno.PostarPedidoAsync(NovoPedido(), "b1")).StatusCode.ShouldBe(HttpStatusCode.Created,
            "a partição é o cliente autenticado, não o IP");
    }

    [Fact]
    public async Task Catalogo_BaldeVazio_Retorna429SoParaAqueleIp()
    {
        await using var api = new ApiFactory(l => l.Catalogo.Capacidade = 3);
        using var ipA = api.ClienteComChave(null, ip: "203.0.113.10");
        using var ipB = api.ClienteComChave(null, ip: "203.0.113.20");

        for (var i = 0; i < 3; i++)
            (await ipA.GetAsync("/catalogo", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var rejeitada = await ipA.GetAsync("/catalogo", Ct);

        rejeitada.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests, "hit de cache também consome cota");
        rejeitada.Headers.RetryAfter.ShouldNotBeNull();
        (await ipB.GetAsync("/catalogo", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConsultarPedido_JanelaDeslizante_LimitaPorCliente()
    {
        await using var api = new ApiFactory(l => l.Consultas.Limite = 2);
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);
        using var bruno = api.ClienteComChave(ApiFactory.ChaveClienteB);
        var url = $"/pedidos/{Guid.NewGuid()}";

        (await ana.GetAsync(url, Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ana.GetAsync(url, Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ana.GetAsync(url, Ct)).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await bruno.GetAsync(url, Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Politicas_SaoIndependentesPorEndpoint()
    {
        await using var api = new ApiFactory(l => l.Pedidos.Limite = 1);
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);

        var criado = await ana.PostarPedidoAsync(NovoPedido(), "x1");
        (await ana.PostarPedidoAsync(NovoPedido(), "x2")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        (await ana.GetAsync(criado.Headers.Location, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK,
            "esgotar a cota de escrita não bloqueia a leitura");
    }

    [Fact]
    public async Task Relatorio_SegundaRequisicaoSimultanea_Retorna429ELiberaAoTerminar()
    {
        var gerador = new GeradorComPortao();
        await using var api = new ApiFactory(servicos: s => s.AddSingleton<IGeradorDeRelatorio>(gerador));
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);
        using var bruno = api.ClienteComChave(ApiFactory.ChaveClienteB);

        var primeira = ana.GetAsync("/relatorios/vendas", Ct);
        try
        {
            await gerador.Entrou.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); // a 1ª está DENTRO do relatório

            var segunda = await bruno.GetAsync("/relatorios/vendas", Ct);
            segunda.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests, "concorrência é global: 1 relatório por vez");
        }
        finally
        {
            gerador.Liberar.TrySetResult(); // nunca deixa requisição presa, nem quando o teste falha
        }

        (await primeira.WaitAsync(TimeSpan.FromSeconds(10), Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await bruno.GetAsync("/relatorios/vendas", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK, "a permissão voltou");
    }

    /// <summary>Gerador que avisa quando entrou e só termina quando o teste libera (coordenação sem Sleep).</summary>
    private sealed class GeradorComPortao : IGeradorDeRelatorio
    {
        public TaskCompletionSource Entrou { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _chamadas;

        public async Task<RelatorioDeVendas> GerarAsync(CancellationToken ct)
        {
            if (Interlocked.Increment(ref _chamadas) == 1)
            {
                Entrou.SetResult();
                await Liberar.Task.WaitAsync(ct);
            }
            return new RelatorioDeVendas(0, DateTimeOffset.UnixEpoch);
        }
    }
}
