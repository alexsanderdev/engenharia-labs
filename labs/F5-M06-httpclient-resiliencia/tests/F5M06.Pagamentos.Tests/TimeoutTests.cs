using F5M06.Pagamentos.Gateway;
using F5M06.Pagamentos.Tests.Infra;
using static F5M06.Pagamentos.Tests.Infra.GatewayFake;

namespace F5M06.Pagamentos.Tests;

/// <summary>Passo 6 — timeout por tentativa e orçamento total.</summary>
public sealed class TimeoutTests
{
    [Fact]
    public async Task Cobrar_GatewayLento_RespeitaTimeoutPorTentativaETimeoutTotal()
    {
        // O gateway demora 2 s REAIS para responder; o relógio do pipeline é falso.
        // Linha do tempo (fake): t=0 tentativa 1 · t=1 timeout → tentativa 2 · t=2 timeout → tentativa 3 · t=2,5 orçamento total estoura.
        using var c = new Cenario(o =>
        {
            o.TimeoutPorTentativa = TimeSpan.FromSeconds(1);
            o.TimeoutTotal = TimeSpan.FromSeconds(2.5);
            o.MaxRetentativas = 5;
        });
        c.Gateway.Sempre(PostCobranca(), Aprovada().WithDelay(TimeSpan.FromSeconds(2)));
        var inicio = c.Relogio.GetUtcNow();
        var umSegundo = TimeSpan.FromSeconds(1);

        var tarefa = c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "chave-1");

        await c.Relogio.AguardarAgendamentoAsync(umSegundo, ocorrencia: 1);
        c.Relogio.Advance(umSegundo);
        await c.Relogio.AguardarAgendamentoAsync(umSegundo, ocorrencia: 2);
        c.Relogio.Advance(umSegundo);
        await c.Relogio.AguardarAgendamentoAsync(umSegundo, ocorrencia: 3);
        c.Relogio.Advance(TimeSpan.FromMilliseconds(500));

        var resultado = await tarefa.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        resultado.ShouldBe(new ResultadoCobranca.Indisponivel(MotivoIndisponibilidade.Timeout));
        (c.Relogio.GetUtcNow() - inicio).ShouldBe(TimeSpan.FromSeconds(2.5));
        c.Relogio.Agendamentos.ShouldContain(TimeSpan.FromSeconds(2.5), "timeout total agendado");
        c.Relogio.Agendamentos.Count(t => t == umSegundo).ShouldBe(3, "3 tentativas cabem no orçamento, não 6");
    }
}
