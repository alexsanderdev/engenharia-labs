using F4M04.Cqrs.Pedidos.Escrita;
using F4M04.Cqrs.Pedidos.Infra;
using F4M04.Cqrs.Tests.Infra;

namespace F4M04.Cqrs.Tests;

/// <summary>Passo 1: o dispatcher encontra o handler certo a partir do tipo da mensagem.</summary>
public sealed class DispatcherTests
{
    [Fact]
    public async Task SendAsync_CriarPedido_RoteiaParaOHandlerEDevolveOIdCriado()
    {
        await using var ambiente = new Ambiente();

        var id = await ambiente.EnviarAsync(new CriarPedido(Guid.NewGuid(), [new ItemSolicitado(ProdutosConhecidos.Mouse.Id, 1)]));

        id.ShouldNotBe(Guid.Empty);
        ambiente.Escrita.Pedidos.ShouldContainKey(id);
    }

    [Fact]
    public async Task SendAsync_DoisCommandsComOMesmoTipoDeResultado_CadaUmVaiParaOSeuHandler()
    {
        await using var ambiente = new Ambiente(comTiposDeTeste: true);

        (await ambiente.EnviarAsync(new ComandoDeTeste("abc"))).ShouldBe("ABC");
        (await ambiente.EnviarAsync(new OutroComandoDeTeste(7))).ShouldBe("outro:7");
    }

    [Fact]
    public async Task QueryAsync_QuerySemHandler_LancaInvalidOperationComONomeDaQuery()
    {
        await using var ambiente = new Ambiente(comTiposDeTeste: true);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => ambiente.ConsultarAsync(new QuerySemHandler()));

        ex.Message.ShouldContain(nameof(QuerySemHandler));
    }
}
