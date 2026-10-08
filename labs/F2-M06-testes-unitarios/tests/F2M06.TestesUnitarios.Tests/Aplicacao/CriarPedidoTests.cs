using F2M06.TestesUnitarios.Aplicacao;
using F2M06.TestesUnitarios.Dominio;
using F2M06.TestesUnitarios.Tests.Dubles;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static F2M06.TestesUnitarios.Tests.Builders.PedidoBuilder;
using static F2M06.TestesUnitarios.Tests.Builders.ProdutoBuilder;

namespace F2M06.TestesUnitarios.Tests.Aplicacao;

/// <summary>
/// Passo 5: o caso de uso inteiro como UMA unidade de comportamento (escola clássica / Khorikov).
/// Cada dependência recebe o dublê certo:
///   - repositório (gerenciada)     → FAKE em memória, verificamos ESTADO;
///   - catálogo (consulta)          → STUB do NSubstitute, nunca verificado;
///   - publicador (não gerenciada)  → MOCK do NSubstitute, verificamos a INTERAÇÃO;
///   - relógio                      → FakeTimeProvider, tempo fixo e controlável.
/// </summary>
public sealed class CriarPedidoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 8, 19, 30, 0, TimeSpan.Zero);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly Guid _cliente = Guid.NewGuid();
    private readonly Produto _pizza = UmProduto().ComNome("Pizza").ComPreco(50m);
    private readonly Produto _refri = UmProduto().ComNome("Refrigerante").ComPreco(8m);
    private readonly FakeTimeProvider _relogio = new(Agora);
    private readonly IPublicadorDeEventos _publicador = Substitute.For<IPublicadorDeEventos>();
    private RepositorioDePedidosEmMemoria _repositorio = new();

    private CriarPedido CriarSut() =>
        new(_repositorio, CatalogoStub.Com(_pizza, _refri), _publicador, _relogio);

    private CriarPedidoComando Comando(params ItemDoComando[] itens) => new(_cliente, itens);

    private static ItemDoComando Item(Guid produtoId, int quantidade) => new(produtoId, quantidade);

    [Fact]
    public async Task Executar_ComandoValido_SalvaOPedidoComTotalCalculadoNoServidor()
    {
        // Arrange
        var sut = CriarSut();
        var comando = Comando(Item(_pizza.Id, 2), Item(_refri.Id, 2));

        // Act
        var resultado = await sut.ExecutarAsync(comando, Ct);

        // Assert — estado, via fake
        resultado.Sucesso.ShouldBeTrue(resultado.Erro);
        var salvo = (await _repositorio.ObterPorIdAsync(resultado.PedidoId, Ct)).ShouldNotBeNull();
        salvo.ClienteId.ShouldBe(_cliente);
        salvo.Total.ShouldBe(116m);
        resultado.Total.ShouldBe(116m);
    }

    [Fact]
    public async Task Executar_ComandoValido_UsaAHoraDoRelogio()
    {
        var sut = CriarSut();

        var resultado = await sut.ExecutarAsync(Comando(Item(_pizza.Id, 1)), Ct);

        var salvo = (await _repositorio.ObterPorIdAsync(resultado.PedidoId, Ct)).ShouldNotBeNull();
        salvo.CriadoEm.ShouldBe(Agora);
        salvo.ExpiraEm.ShouldBe(Agora.AddMinutes(30));
    }

    [Fact]
    public async Task Executar_ComandoValido_PublicaPedidoCriadoUmaVez()
    {
        var sut = CriarSut();

        var resultado = await sut.ExecutarAsync(Comando(Item(_pizza.Id, 1), Item(_refri.Id, 1)), Ct);

        // Interação, via mock — só porque o evento sai do sistema.
        _publicador.DeveTerPublicadoUmaVez(new PedidoCriado(resultado.PedidoId, _cliente, 58m, Agora));
    }

    [Fact]
    public async Task Executar_SemItens_FalhaSemSalvarNemPublicar()
    {
        var sut = CriarSut();

        var resultado = await sut.ExecutarAsync(Comando(), Ct);

        resultado.Sucesso.ShouldBeFalse();
        resultado.Erro.ShouldBe("Pedido precisa de ao menos um item.");
        _repositorio.Pedidos.ShouldBeEmpty();
        _publicador.NaoDeveTerPublicadoNada();
    }

    [Fact]
    public async Task Executar_ProdutoForaDoCatalogo_FalhaSemSalvarNemPublicar()
    {
        var sut = CriarSut();
        var inexistente = Guid.NewGuid();

        var resultado = await sut.ExecutarAsync(Comando(Item(_pizza.Id, 1), Item(inexistente, 1)), Ct);

        resultado.Sucesso.ShouldBeFalse();
        resultado.Erro.ShouldBe($"Produto {inexistente} não encontrado.");
        _repositorio.Pedidos.ShouldBeEmpty();
        _publicador.NaoDeveTerPublicadoNada();
    }

    [Fact]
    public async Task Executar_ProdutoInativo_FalhaComAMensagemDoDominio()
    {
        _pizza.Desativar();
        var sut = CriarSut();

        var resultado = await sut.ExecutarAsync(Comando(Item(_pizza.Id, 1)), Ct);

        resultado.Sucesso.ShouldBeFalse();
        resultado.Erro.ShouldBe("Produto 'Pizza' está inativo.");
        _publicador.NaoDeveTerPublicadoNada();
    }

    [Fact]
    public async Task Executar_ClienteNoLimiteDePedidosEmAberto_Falha()
    {
        _repositorio = new RepositorioDePedidosEmMemoria(
            UmPedido().DoCliente(_cliente),
            UmPedido().DoCliente(_cliente),
            UmPedido().DoCliente(_cliente).Confirmado());
        var sut = CriarSut();

        var resultado = await sut.ExecutarAsync(Comando(Item(_pizza.Id, 1)), Ct);

        resultado.Sucesso.ShouldBeFalse();
        resultado.Erro!.ShouldContain("limite: 3");
        _repositorio.Pedidos.Count.ShouldBe(3);
        _publicador.NaoDeveTerPublicadoNada();
    }

    [Fact]
    public async Task Executar_PedidosCanceladosNaoContamNoLimite()
    {
        _repositorio = new RepositorioDePedidosEmMemoria(
            UmPedido().DoCliente(_cliente),
            UmPedido().DoCliente(_cliente),
            UmPedido().DoCliente(_cliente).Cancelado());
        var sut = CriarSut();

        var resultado = await sut.ExecutarAsync(Comando(Item(_pizza.Id, 1)), Ct);

        resultado.Sucesso.ShouldBeTrue(resultado.Erro);
        _repositorio.Pedidos.Count.ShouldBe(4);
    }
}
