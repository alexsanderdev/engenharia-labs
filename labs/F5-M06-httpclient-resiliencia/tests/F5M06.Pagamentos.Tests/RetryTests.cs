using F5M06.Pagamentos.Gateway;
using F5M06.Pagamentos.Tests.Infra;
using WireMock.ResponseBuilders;
using static F5M06.Pagamentos.Tests.Infra.GatewayFake;

namespace F5M06.Pagamentos.Tests;

/// <summary>Passo 5 — retry só para transitório + idempotente, Retry-After e backoff com jitter.</summary>
public sealed class RetryTests
{
    [Fact]
    public async Task Cobrar_FalhaIntermitente_RetentaComAMesmaIdempotencyKeyEAprova()
    {
        using var c = new Cenario();
        c.Gateway.EmSequencia(PostCobranca(), Status(503), Status(500), Aprovada("tx_42"));

        var resultado = await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "pedido-abc-pagamento-1");

        resultado.ShouldBe(new ResultadoCobranca.Aprovada("tx_42"));
        var requisicoes = await c.Gateway.RequisicoesAsync(RotaCobrancas, 3);
        requisicoes.Count.ShouldBe(3);
        requisicoes.ShouldAllBe(r => Header(r, ResilienciaGateway.CabecalhoIdempotencia) == "pedido-abc-pagamento-1");
        requisicoes.Select(r => Header(r, CorrelacaoEApiKeyHandler.CabecalhoCorrelacao)).Distinct().Count()
            .ShouldBe(1, "todas as tentativas pertencem à mesma chamada lógica");
        requisicoes.Select(r => r.Body).Distinct().Count().ShouldBe(1, "o corpo reenviado é idêntico");
    }

    [Theory]
    [InlineData("erro-500", MotivoIndisponibilidade.ErroNoGateway, 4)]
    [InlineData("conexao-derrubada", MotivoIndisponibilidade.FalhaDeRede, 4)]
    [InlineData("corpo-corrompido", MotivoIndisponibilidade.RespostaInvalida, 1)]
    public async Task Cobrar_GatewayFalhando_ViraIndisponivelSemVazarExcecao(string falha, MotivoIndisponibilidade esperado, int tentativas)
    {
        // 500 e conexão derrubada são transitórios: 1 tentativa + 3 retentativas.
        // 200 com corpo corrompido NÃO é falha para o pipeline (o HTTP deu certo): não há retry,
        // mas o client também não pode deixar a JsonException escapar.
        using var derrubador = new ServidorQueDerrubaConexoes();
        using var c = new Cenario(o =>
        {
            o.MaxRetentativas = 3;
            if (falha == "conexao-derrubada")
            {
                o.BaseAddress = derrubador.Url;
            }
        });
        c.Gateway.Sempre(PostCobranca(), falha == "erro-500"
            ? Status(500)
            : Response.Create().WithFault(FaultType.MALFORMED_RESPONSE_CHUNK));

        var resultado = await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "chave-1");

        resultado.ShouldBe(new ResultadoCobranca.Indisponivel(esperado));
        var recebidas = falha == "conexao-derrubada"
            ? derrubador.Conexoes
            : (await c.Gateway.RequisicoesAsync(RotaCobrancas, tentativas)).Count;
        recebidas.ShouldBe(tentativas);
    }

    [Fact]
    public async Task Estornar_PostSemIdempotencyKey_NaoERetentado()
    {
        using var c = new Cenario(o => o.MaxRetentativas = 3);
        c.Gateway.Sempre(PostEstorno("tx_1"), Status(503));

        var resultado = await c.Cliente.EstornarAsync("tx_1");

        resultado.ShouldBe(new ResultadoEstorno(false, MotivoIndisponibilidade.ErroNoGateway));
        (await c.Gateway.RequisicoesAsync(RotaEstorno("tx_1"), 1)).Count
            .ShouldBe(1, "POST sem chave pode ter sido processado: repetir pode estornar duas vezes");
    }

    [Theory]
    [InlineData(429)]
    [InlineData(503)]
    public async Task Cobrar_ComRetryAfter_EsperaExatamenteOTempoPedidoPeloGateway(int status)
    {
        using var c = new Cenario();
        c.Gateway.EmSequencia(PostCobranca(), Status(status).WithHeader("Retry-After", "3"), Aprovada());

        var tarefa = c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "chave-1");

        await c.Relogio.AguardarAgendamentoAsync(TimeSpan.FromSeconds(3)); // retry parado esperando o Retry-After
        c.Relogio.Advance(TimeSpan.FromSeconds(2.9));
        await Task.Delay(50, TestContext.Current.CancellationToken);
        tarefa.IsCompleted.ShouldBeFalse("ainda faltam 100 ms do Retry-After");
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 1)).Count.ShouldBe(1);

        c.Relogio.Advance(TimeSpan.FromMilliseconds(100));
        (await tarefa.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken))
            .ShouldBeOfType<ResultadoCobranca.Aprovada>();
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 2)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Cobrar_SemRetryAfter_EsperaComBackoffExponencialComJitter()
    {
        using var c = new Cenario(o =>
        {
            o.MaxRetentativas = 3;
            o.AtrasoBase = TimeSpan.FromMilliseconds(200);
        });
        c.Gateway.Sempre(PostCobranca(), Status(503));

        var tarefa = c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "chave-1");

        var atrasos = new List<TimeSpan>();
        for (var i = 1; i <= 3; i++)
        {
            // timers menores que o timeout por tentativa (5 s) são as esperas do retry
            var atraso = await c.Relogio.AguardarAgendamentoAsync(t => t < TimeSpan.FromSeconds(5), i);
            atrasos.Add(atraso);
            c.Relogio.Advance(atraso);
        }

        (await tarefa.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken))
            .ShouldBe(new ResultadoCobranca.Indisponivel(MotivoIndisponibilidade.ErroNoGateway));
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 4)).Count.ShouldBe(4);
        atrasos.ShouldAllBe(a => a > TimeSpan.Zero && a < TimeSpan.FromSeconds(5));
        atrasos.Distinct().Count().ShouldBe(3, "com jitter, as esperas não são valores fixos");
        atrasos.Sum(a => a.TotalMilliseconds).ShouldBeGreaterThan(200, "backoff exponencial: as esperas crescem a partir do atraso base");
    }
}
