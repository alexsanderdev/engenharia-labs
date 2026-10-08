using F4M08.Api.Pedidos;
using F4M08.Api.Resultados;
using Microsoft.Extensions.Time.Testing;

namespace F4M08.Api.Tests;

/// <summary>
/// Passo 2: os casos de uso, sem HTTP. Nenhum destes testes espera exceção:
/// todo "não deu" de negócio volta como Result com Error tipado e código estável.
/// </summary>
public sealed class CasosDeUsoTests
{
    private static readonly Guid Ana = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Bruno = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Agora = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly PedidosCasosDeUso _casos = new(new CatalogoEmMemoria(), new PedidoRepositorioEmMemoria(), new FakeTimeProvider(Agora));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<PedidoResponse> CriarAsync(Guid cliente) =>
        (await _casos.CriarAsync(new CriarPedidoRequest(cliente, [new ItemPedidoRequest(ProdutosConhecidos.Teclado.Id, 2)]), Ct)).Value;

    [Fact]
    public async Task Criar_Valido_CalculaTotalNoServidor()
    {
        var resultado = await _casos.CriarAsync(new CriarPedidoRequest(Ana,
        [
            new ItemPedidoRequest(ProdutosConhecidos.Teclado.Id, 2),
            new ItemPedidoRequest(ProdutosConhecidos.Mouse.Id, 1),
        ]), Ct);

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Total.ShouldBe(620.00m);
        resultado.Value.Status.ShouldBe("Created");
        resultado.Value.CriadoEm.ShouldBe(Agora);
        (await _casos.ObterAsync(resultado.Value.Id, Ct)).Value.Id.ShouldBe(resultado.Value.Id);
    }

    [Fact]
    public async Task Criar_EntradaInvalida_RetornaValidationComErrosPorCampo()
    {
        var semNada = await _casos.CriarAsync(new CriarPedidoRequest(Guid.Empty, null), Ct);
        var quantidadeZero = await _casos.CriarAsync(new CriarPedidoRequest(Ana,
            [new ItemPedidoRequest(ProdutosConhecidos.Mouse.Id, 1), new ItemPedidoRequest(ProdutosConhecidos.Mouse.Id, 0)]), Ct);

        semNada.Error.Type.ShouldBe(ErrorType.Validation);
        semNada.Error.Code.ShouldBe(PedidoErrors.CodigoValidacao);
        semNada.Error.ValidationErrors!.Keys.ShouldBe(["clienteId", "itens"], ignoreOrder: true);
        quantidadeZero.Error.ValidationErrors!.Keys.ShouldBe(["itens[1].quantidade"]);
    }

    [Fact]
    public async Task Criar_ProdutoInativoOuInexistente_RetornaFailureComCodigoEstavel()
    {
        var inativo = await _casos.CriarAsync(new CriarPedidoRequest(Ana, [new ItemPedidoRequest(ProdutosConhecidos.Webcam.Id, 1)]), Ct);
        var inexistente = await _casos.CriarAsync(new CriarPedidoRequest(Ana, [new ItemPedidoRequest(Guid.NewGuid(), 1)]), Ct);

        inativo.Error.Type.ShouldBe(ErrorType.Failure);
        inativo.Error.Code.ShouldBe(PedidoErrors.CodigoProdutoInativo);
        inexistente.Error.Type.ShouldBe(ErrorType.Failure);
        inexistente.Error.Code.ShouldBe(PedidoErrors.CodigoProdutoInexistente);
    }

    [Fact]
    public async Task Transicoes_InvalidasOuSemPermissao_RetornamErrosTipadosSemLancar()
    {
        var pedido = await CriarAsync(Ana);

        (await _casos.ObterAsync(Guid.NewGuid(), Ct)).Error.Type.ShouldBe(ErrorType.NotFound);
        (await _casos.ConfirmarAsync(Guid.NewGuid(), Ct)).Error.Code.ShouldBe(PedidoErrors.CodigoNaoEncontrado);

        (await _casos.ConfirmarAsync(pedido.Id, Ct)).Value.Status.ShouldBe("Confirmed");
        (await _casos.ConfirmarAsync(pedido.Id, Ct)).Error.Type.ShouldBe(ErrorType.Conflict);

        var deOutro = await _casos.CancelarAsync(pedido.Id, Bruno, Ct);
        deOutro.Error.Type.ShouldBe(ErrorType.Forbidden);
        deOutro.Error.Code.ShouldBe(PedidoErrors.CodigoAcessoNegado);

        (await _casos.ConcluirAsync(pedido.Id, Ct)).Value.Status.ShouldBe("Completed");
        var completedNaoCancela = await _casos.CancelarAsync(pedido.Id, Ana, Ct);
        completedNaoCancela.Error.Code.ShouldBe(PedidoErrors.CodigoTransicaoInvalida);
        (await _casos.ObterAsync(pedido.Id, Ct)).Value.Status.ShouldBe("Completed");
    }
}
