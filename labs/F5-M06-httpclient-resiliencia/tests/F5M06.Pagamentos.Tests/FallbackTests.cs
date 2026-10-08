using F5M06.Pagamentos.Gateway;
using F5M06.Pagamentos.Tests.Infra;
using static F5M06.Pagamentos.Tests.Infra.GatewayFake;

namespace F5M06.Pagamentos.Tests;

/// <summary>Passo 8 — fallback na consulta de status (leitura degradada é melhor que erro).</summary>
public sealed class FallbackTests
{
    [Fact]
    public async Task ConsultarStatus_GatewayCaiDepoisDeResponder_UsaUltimoStatusConhecido()
    {
        using var c = new Cenario();
        c.Gateway.EmSequencia(GetStatus("tx_1"), StatusDaTransacao("tx_1", "aprovada"), Status(503));

        var primeira = await c.Cliente.ConsultarStatusAsync("tx_1");
        var segunda = await c.Cliente.ConsultarStatusAsync("tx_1");

        primeira.ShouldBe(new ConsultaStatus("tx_1", StatusPagamento.Aprovado, OrigemStatus.Gateway));
        segunda.ShouldBe(new ConsultaStatus("tx_1", StatusPagamento.Aprovado, OrigemStatus.UltimoConhecido));
        segunda.Degradado.ShouldBeTrue();
        (await c.Gateway.RequisicoesAsync(RotaStatus("tx_1"), 4)).Count.ShouldBe(4, "1 sucesso + 3 tentativas do GET (idempotente)");
    }

    [Fact]
    public async Task ConsultarStatus_GatewayForaSemHistorico_RetornaDesconhecidoSemLancar()
    {
        using var c = new Cenario();
        c.Gateway.Sempre(GetStatus("tx_9"), Status(500));

        var consulta = await c.Cliente.ConsultarStatusAsync("tx_9");

        consulta.ShouldBe(new ConsultaStatus("tx_9", StatusPagamento.Desconhecido, OrigemStatus.Padrao));
        (await c.Gateway.RequisicoesAsync(RotaStatus("tx_9"), 3)).Count.ShouldBe(3);
    }
}
