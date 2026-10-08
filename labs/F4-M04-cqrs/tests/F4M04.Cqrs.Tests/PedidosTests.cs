using F4M04.Cqrs.Pedidos.Dominio;
using F4M04.Cqrs.Pedidos.Escrita;
using F4M04.Cqrs.Pedidos.Infra;
using F4M04.Cqrs.Pedidos.Leitura;
using F4M04.Cqrs.Tests.Infra;

namespace F4M04.Cqrs.Tests;

/// <summary>Passo 6: os casos de uso de Pedidos de ponta a ponta (command → commit → evento → projeção → query).</summary>
public sealed class PedidosTests
{
    private static readonly Guid Ana = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    [Fact]
    public async Task CriarPedido_CalculaTotalNoServidorEApareceNoReadModel()
    {
        await using var ambiente = new Ambiente();

        var id = await ambiente.EnviarAsync(new CriarPedido(Ana,
        [
            new ItemSolicitado(ProdutosConhecidos.Teclado.Id, 2),
            new ItemSolicitado(ProdutosConhecidos.Mouse.Id, 1),
        ]));

        var resumo = await ambiente.ConsultarAsync(new ObterPedido(id));
        resumo.ShouldNotBeNull();
        resumo.ShouldBe(new PedidoResumo(id, Ana, "Created", 620.00m, 3, Ambiente.Inicio, ConfirmadoEm: null));
    }

    [Fact]
    public async Task CriarPedido_ProdutoInativo_LancaRegraDeNegocioENadaEGravado()
    {
        await using var ambiente = new Ambiente();

        await Should.ThrowAsync<RegraDeNegocioException>(() => ambiente.EnviarAsync(
            new CriarPedido(Ana, [new ItemSolicitado(ProdutosConhecidos.Webcam.Id, 1)])));

        ambiente.Escrita.Commits.ShouldBe(0);
        ambiente.Escrita.Pedidos.ShouldBeEmpty();
        ambiente.Leitura.Pedidos.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConfirmarPedido_AtualizaStatusEDataNoReadModelPreservandoOResto()
    {
        await using var ambiente = new Ambiente();
        var id = await ambiente.EnviarAsync(new CriarPedido(Ana, [new ItemSolicitado(ProdutosConhecidos.Teclado.Id, 1)]));
        ambiente.Relogio.Advance(TimeSpan.FromHours(1));

        await ambiente.EnviarAsync(new ConfirmarPedido(id));

        var resumo = (await ambiente.ConsultarAsync(new ObterPedido(id)))!;
        resumo.Status.ShouldBe("Confirmed");
        resumo.ConfirmadoEm.ShouldBe(Ambiente.Inicio.AddHours(1));
        resumo.Total.ShouldBe(250.00m);
        resumo.CriadoEm.ShouldBe(Ambiente.Inicio);
        ambiente.Escrita.Pedidos[id].Status.ShouldBe(StatusPedido.Confirmed);
    }

    [Fact]
    public async Task ConfirmarPedido_InexistenteOuJaConfirmado_LancaExcecaoDoDominio()
    {
        await using var ambiente = new Ambiente();
        var id = await ambiente.EnviarAsync(new CriarPedido(Ana, [new ItemSolicitado(ProdutosConhecidos.Mouse.Id, 1)]));
        await ambiente.EnviarAsync(new ConfirmarPedido(id));

        var inexistente = Guid.NewGuid();
        var naoEncontrado = await Should.ThrowAsync<PedidoNaoEncontradoException>(() => ambiente.EnviarAsync(new ConfirmarPedido(inexistente)));
        naoEncontrado.PedidoId.ShouldBe(inexistente);
        await Should.ThrowAsync<RegraDeNegocioException>(() => ambiente.EnviarAsync(new ConfirmarPedido(id)));
    }
}
