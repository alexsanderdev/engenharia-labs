using F5M06.Pagamentos.Gateway;
using F5M06.Pagamentos.Tests.Infra;
using static F5M06.Pagamentos.Tests.Infra.GatewayFake;

namespace F5M06.Pagamentos.Tests;

/// <summary>Passo 7 — circuit breaker: fechado → aberto → meio-aberto → fechado/aberto.</summary>
public sealed class CircuitBreakerTests
{
    private static Cenario CenarioComCircuito() => new(o =>
    {
        o.MaxRetentativas = 2;            // 3 tentativas por chamada
        o.CircuitoVazaoMinima = 4;        // avalia a partir de 4 chamadas na janela
        o.CircuitoTaxaDeFalhas = 0.5;
        o.CircuitoDuracaoAberto = TimeSpan.FromSeconds(30);
    });

    private static readonly ResultadoCobranca CircuitoAberto = new ResultadoCobranca.Indisponivel(MotivoIndisponibilidade.CircuitoAberto);

    [Fact]
    public async Task Circuito_AbreAposFalhasSeguidas_ERejeitaSemChamarOGateway()
    {
        using var c = CenarioComCircuito();
        c.Gateway.Sempre(PostCobranca(), Status(500));

        (await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "k1"))
            .ShouldBe(new ResultadoCobranca.Indisponivel(MotivoIndisponibilidade.ErroNoGateway));
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 3)).Count.ShouldBe(3, "3 falhas: ainda abaixo da vazão mínima");

        (await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "k2")).ShouldBe(CircuitoAberto);
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 4)).Count.ShouldBe(4, "a 4ª falha abre o circuito; a retentativa nem sai");

        (await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "k3")).ShouldBe(CircuitoAberto);
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 4)).Count.ShouldBe(4, "circuito aberto falha rápido, sem tocar no gateway");
    }

    [Fact]
    public async Task Circuito_DepoisDoBreak_MeioAbre_FalhaReabre_ESucessoFecha()
    {
        using var c = CenarioComCircuito();
        c.Gateway.Sempre(PostCobranca(), Status(500));
        await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "k1");
        (await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "k2")).ShouldBe(CircuitoAberto);

        c.Relogio.Advance(TimeSpan.FromSeconds(30)); // meio-aberto: deixa UMA chamada de teste passar
        (await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "k3")).ShouldBe(CircuitoAberto);
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 5)).Count.ShouldBe(5, "a chamada de teste falhou e o circuito reabriu");
        (await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "k4")).ShouldBe(CircuitoAberto);
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 5)).Count.ShouldBe(5);

        c.Gateway.LimparRespostas();
        c.Gateway.Sempre(PostCobranca(), Aprovada());
        c.Relogio.Advance(TimeSpan.FromSeconds(30));
        (await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "k5")).ShouldBeOfType<ResultadoCobranca.Aprovada>();
        (await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "k6")).ShouldBeOfType<ResultadoCobranca.Aprovada>();
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 7)).Count.ShouldBe(7, "chamada de teste ok: circuito fechou");
    }
}
