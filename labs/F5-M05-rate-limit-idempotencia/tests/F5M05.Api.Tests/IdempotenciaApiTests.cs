using System.Net;
using F5M05.Api.Catalogo;
using F5M05.Api.Pedidos;
using F5M05.Api.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;
using static F5M05.Api.Tests.Infra.Http;

namespace F5M05.Api.Tests;

/// <summary>
/// Parte (b), Passo 5 — Idempotency-Key no POST /pedidos, de ponta a ponta.
/// O cliente (app, integração) repete o POST quando a rede falha; sem idempotência, cada retry é um pedido a mais.
/// </summary>
public sealed class IdempotenciaApiTests
{
    [Fact]
    public async Task CriarPedido_SemIdempotencyKey_Retorna400()
    {
        await using var api = new ApiFactory();
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);

        var resposta = await ana.PostarPedidoAsync(NovoPedido(), chaveDeIdempotencia: null);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        api.Pedidos.Quantidade.ShouldBe(0);
    }

    [Fact]
    public async Task CriarPedido_MesmaChaveMesmoCorpo_DevolveAMesmaRespostaSemDuplicar()
    {
        await using var api = new ApiFactory();
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);
        var chave = Guid.NewGuid().ToString();

        var primeira = await ana.PostarPedidoAsync(NovoPedido(2), chave);
        var retry = await ana.PostarPedidoAsync(NovoPedido(2), chave);

        primeira.StatusCode.ShouldBe(HttpStatusCode.Created);
        retry.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await retry.IdDoPedidoAsync()).ShouldBe(await primeira.IdDoPedidoAsync());
        retry.Headers.Location.ShouldBe(primeira.Headers.Location);
        retry.Headers.GetValues("Idempotent-Replayed").ShouldBe(["true"]);
        api.Pedidos.Quantidade.ShouldBe(1);
    }

    [Fact]
    public async Task CriarPedido_MesmaChaveCorpoDiferente_Retorna422()
    {
        await using var api = new ApiFactory();
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);

        await ana.PostarPedidoAsync(NovoPedido(1), "reuso");
        var resposta = await ana.PostarPedidoAsync(NovoPedido(5), "reuso");

        resposta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        api.Pedidos.Quantidade.ShouldBe(1);
    }

    [Fact]
    public async Task CriarPedido_MesmaChaveDeOutroCliente_NaoReaproveitaAResposta()
    {
        await using var api = new ApiFactory();
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);
        using var bruno = api.ClienteComChave(ApiFactory.ChaveClienteB);

        var idDaAna = await (await ana.PostarPedidoAsync(NovoPedido(), "pedido-1")).IdDoPedidoAsync();
        var doBruno = await bruno.PostarPedidoAsync(NovoPedido(), "pedido-1");

        doBruno.StatusCode.ShouldBe(HttpStatusCode.Created);
        var corpoDoBruno = await doBruno.JsonAsync();
        corpoDoBruno.GetProperty("id").GetGuid().ShouldNotBe(idDaAna, "a chave é escopada por cliente");
        corpoDoBruno.GetProperty("clienteId").GetString().ShouldBe("cliente-b");

        var retryDaAna = await ana.PostarPedidoAsync(NovoPedido(), "pedido-1");
        (await retryDaAna.IdDoPedidoAsync()).ShouldBe(idDaAna, "a Ana continua recebendo o pedido DELA");
        api.Pedidos.Quantidade.ShouldBe(2);
    }

    [Fact]
    public async Task CriarPedido_SegundaRequisicaoEnquantoAPrimeiraProcessa_Retorna409EDepoisReplay()
    {
        var repositorio = new RepositorioComPortao();
        await using var api = new ApiFactory(servicos: s => s.AddSingleton<IRepositorioDePedidos>(repositorio));
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);

        var primeira = ana.PostarPedidoAsync(NovoPedido(), "concorrente");
        try
        {
            await repositorio.Entrou.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); // a 1ª está gravando

            var segunda = await ana.PostarPedidoAsync(NovoPedido(), "concorrente");
            segunda.StatusCode.ShouldBe(HttpStatusCode.Conflict, "só uma requisição por chave processa");
            segunda.Headers.RetryAfter.ShouldNotBeNull("o cliente precisa saber que pode tentar de novo");
        }
        finally
        {
            repositorio.Liberar.TrySetResult(); // nunca deixa requisição presa, nem quando o teste falha
        }

        var resultado = await primeira.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        resultado.StatusCode.ShouldBe(HttpStatusCode.Created);

        var terceira = await ana.PostarPedidoAsync(NovoPedido(), "concorrente");
        (await terceira.IdDoPedidoAsync()).ShouldBe(await resultado.IdDoPedidoAsync());
        repositorio.Quantidade.ShouldBe(1);
    }

    [Fact]
    public async Task CriarPedido_RajadaSimultaneaComAMesmaChave_CriaExatamenteUmPedido()
    {
        await using var api = new ApiFactory();
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);

        var respostas = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => ana.PostarPedidoAsync(NovoPedido(), "rajada")));

        respostas.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.Conflict);
        var ids = await Task.WhenAll(respostas.Where(r => r.StatusCode == HttpStatusCode.Created).Select(r => r.IdDoPedidoAsync()));
        ids.ShouldNotBeEmpty();
        ids.Distinct().Count().ShouldBe(1, "todo 201 aponta para o MESMO pedido");
        api.Pedidos.Quantidade.ShouldBe(1);
    }

    [Fact]
    public async Task CriarPedido_FalhaNoServidor_LiberaAChaveEORetryCriaOPedido()
    {
        var repositorio = new RepositorioQueFalhaUmaVez();
        await using var api = new ApiFactory(servicos: s => s.AddSingleton<IRepositorioDePedidos>(repositorio));
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);

        var falhou = await ana.PostarPedidoAsync(NovoPedido(), "retry-seguro");
        var retry = await ana.PostarPedidoAsync(NovoPedido(), "retry-seguro");

        falhou.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        retry.StatusCode.ShouldBe(HttpStatusCode.Created, "5xx não é guardado: o retry processa de verdade");
        retry.Headers.Contains("Idempotent-Replayed").ShouldBeFalse();

        var depoisDoSucesso = await ana.PostarPedidoAsync(NovoPedido(), "retry-seguro");
        depoisDoSucesso.Headers.Contains("Idempotent-Replayed").ShouldBeTrue("o sucesso, sim, é guardado");
        repositorio.Quantidade.ShouldBe(1);
    }

    [Fact]
    public async Task CriarPedido_ChaveExpirada_ProcessaComoNova()
    {
        await using var api = new ApiFactory();
        using var ana = api.ClienteComChave(ApiFactory.ChaveClienteA);

        var original = await ana.PostarPedidoAsync(NovoPedido(), "antiga");
        api.Relogio.Advance(TimeSpan.FromHours(23));
        var aindaGuardada = await ana.PostarPedidoAsync(NovoPedido(), "antiga");
        aindaGuardada.Headers.Contains("Idempotent-Replayed").ShouldBeTrue("23 h: ainda dentro da retenção");

        api.Relogio.Advance(TimeSpan.FromHours(2)); // 25 h: retenção padrão é 24 h
        var depois = await ana.PostarPedidoAsync(NovoPedido(), "antiga");

        depois.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await depois.IdDoPedidoAsync()).ShouldNotBe(await original.IdDoPedidoAsync());
        api.Pedidos.Quantidade.ShouldBe(2);
    }

    // ---------- Dublês ----------

    private sealed class RepositorioComPortao : IRepositorioDePedidos
    {
        private readonly RepositorioDePedidosEmMemoria _real = new();
        public TaskCompletionSource Entrou { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Quantidade => _real.Quantidade;
        private int _chamadas;

        /// <summary>Só a PRIMEIRA gravação espera o portão; as demais passam direto.</summary>
        public async Task AdicionarAsync(Pedido pedido, CancellationToken ct)
        {
            if (Interlocked.Increment(ref _chamadas) == 1)
            {
                Entrou.TrySetResult();
                await Liberar.Task.WaitAsync(ct);
            }
            await _real.AdicionarAsync(pedido, ct);
        }

        public Task<Pedido?> ObterAsync(Guid id, CancellationToken ct) => _real.ObterAsync(id, ct);
    }

    private sealed class RepositorioQueFalhaUmaVez : IRepositorioDePedidos
    {
        private readonly RepositorioDePedidosEmMemoria _real = new();
        private int _tentativas;
        public int Quantidade => _real.Quantidade;

        public Task AdicionarAsync(Pedido pedido, CancellationToken ct) =>
            Interlocked.Increment(ref _tentativas) == 1
                ? throw new TimeoutException("banco indisponível (simulado)")
                : _real.AdicionarAsync(pedido, ct);

        public Task<Pedido?> ObterAsync(Guid id, CancellationToken ct) => _real.ObterAsync(id, ct);
    }
}
