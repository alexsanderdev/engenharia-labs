using F5M06.Pagamentos.Gateway;
using F5M06.Pagamentos.Tests.Infra;
using static F5M06.Pagamentos.Tests.Infra.GatewayFake;

namespace F5M06.Pagamentos.Tests;

/// <summary>Passo 4 — status HTTP e exceções viram resultados de domínio.</summary>
public sealed class MapeamentoTests
{
    [Theory]
    [InlineData(400, MotivoRejeicao.DadosInvalidos)]
    [InlineData(422, MotivoRejeicao.DadosInvalidos)]
    [InlineData(403, MotivoRejeicao.NaoAutorizado)]
    [InlineData(409, MotivoRejeicao.ConflitoDeIdempotencia)]
    public async Task Cobrar_Erro4xx_RetornaRejeitadaSemRetentar(int status, MotivoRejeicao esperado)
    {
        using var c = new Cenario();
        c.Gateway.Sempre(PostCobranca(), Status(status));

        var resultado = await c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "chave-1");

        resultado.ShouldBe(new ResultadoCobranca.Rejeitada(esperado));
        (await c.Gateway.RequisicoesAsync(RotaCobrancas, 1)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Cobrar_CancelamentoDoChamador_PropagaOperationCanceledEmVezDeVirarIndisponivel()
    {
        using var c = new Cenario();
        c.Gateway.Sempre(PostCobranca(), Aprovada().WithDelay(TimeSpan.FromSeconds(2)));
        using var cts = new CancellationTokenSource();

        var tarefa = c.Cliente.CobrarAsync(Cenario.NovaCobranca(), "chave-1", cts.Token);
        await c.Relogio.AguardarAgendamentoAsync(TimeSpan.FromSeconds(5)); // a tentativa começou (timer do timeout por tentativa)
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => tarefa.WaitAsync(TimeSpan.FromSeconds(5)));
    }
}
