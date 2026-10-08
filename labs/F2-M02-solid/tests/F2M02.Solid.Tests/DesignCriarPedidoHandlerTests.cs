using System.Text.Json;

using F2M02.Solid.Aplicacao;
using F2M02.Solid.Descontos;
using F2M02.Solid.Dominio;
using F2M02.Solid.Infraestrutura;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace F2M02.Solid.Tests;

/// <summary>
/// DIP: o caso de uso roda inteiro com dublês (NSubstitute) — nenhuma infraestrutura envolvida.
/// </summary>
public class DesignCriarPedidoHandlerTests
{
    private static readonly Produto Teclado = new(Guid.NewGuid(), "Teclado", 150m, Ativo: true);
    private static readonly Produto Descontinuado = new(Guid.NewGuid(), "Monitor CRT", 300m, Ativo: false);

    private readonly ICatalogoDeProdutos _catalogo = Substitute.For<ICatalogoDeProdutos>();
    private readonly IRepositorioDePedidos _pedidos = Substitute.For<IRepositorioDePedidos>();
    private readonly IOrderEventPublisher _eventos = Substitute.For<IOrderEventPublisher>();
    private readonly INotificadorDeCliente _notificador = Substitute.For<INotificadorDeCliente>();
    private readonly CriarPedidoHandler _handler;

    public DesignCriarPedidoHandlerTests()
    {
        _catalogo.ObterPorIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyList<Produto>>(
                new[] { Teclado, Descontinuado }.Where(p => call.Arg<IReadOnlyCollection<Guid>>().Contains(p.Id)).ToList()));
        _handler = new CriarPedidoHandler(_catalogo, _pedidos, _eventos, _notificador, CatalogoDePoliticasDeDesconto.Padrao());
    }

    private static CriarPedidoCommand Comando(Guid produtoId, int quantidade = 1, string? cupom = null) =>
        new(Guid.NewGuid(), "ana@exemplo.com", [new ItemSolicitado(produtoId, quantidade)], cupom);

    [Fact]
    public async Task Handle_PedidoValido_SalvaPublicaENotifica()
    {
        var ct = TestContext.Current.CancellationToken;

        var pedido = await _handler.HandleAsync(Comando(Teclado.Id, 2, "BLACKFRIDAY"), ct);

        pedido.Total.ShouldBe(240m);
        await _pedidos.Received(1).SalvarAsync(pedido, ct);
        await _eventos.Received(1).PublishOrderCreatedAsync(pedido, ct);
        await _notificador.Received(1).NotificarPedidoCriadoAsync(pedido, "ana@exemplo.com", ct);
    }

    [Fact]
    public async Task Handle_ProdutoInativo_NaoSalvaNaoPublicaNaoNotifica()
    {
        await Should.ThrowAsync<PedidoInvalidoException>(() => _handler.HandleAsync(Comando(Descontinuado.Id), TestContext.Current.CancellationToken));

        await _pedidos.DidNotReceiveWithAnyArgs().SalvarAsync(default!, TestContext.Current.CancellationToken);
        await _eventos.DidNotReceiveWithAnyArgs().PublishOrderCreatedAsync(default!, TestContext.Current.CancellationToken);
        await _notificador.DidNotReceiveWithAnyArgs().NotificarPedidoCriadoAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ProdutoInexistente_Lanca()
    {
        var id = Guid.NewGuid();

        var ex = await Should.ThrowAsync<PedidoInvalidoException>(() => _handler.HandleAsync(Comando(id), TestContext.Current.CancellationToken));

        ex.Message.ShouldBe($"Produto não encontrado: {id}");
    }

    [Fact]
    public async Task Handle_CupomInvalido_FalhaAntesDeConsultarOCatalogo()
    {
        await Should.ThrowAsync<PedidoInvalidoException>(() => _handler.HandleAsync(Comando(Teclado.Id, cupom: "NATAL"), TestContext.Current.CancellationToken));

        await _catalogo.DidNotReceiveWithAnyArgs().ObterPorIdsAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_FalhaAoSalvar_NaoPublicaEventoNemNotifica()
    {
        _pedidos.SalvarAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("banco fora"));

        await Should.ThrowAsync<InvalidOperationException>(() => _handler.HandleAsync(Comando(Teclado.Id), TestContext.Current.CancellationToken));

        await _eventos.DidNotReceiveWithAnyArgs().PublishOrderCreatedAsync(default!, TestContext.Current.CancellationToken);
        await _notificador.DidNotReceiveWithAnyArgs().NotificarPedidoCriadoAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ProdutoRepetidoNoComando_ConsultaOCatalogoUmaVezPorId()
    {
        var comando = new CriarPedidoCommand(Guid.NewGuid(), "ana@exemplo.com", [new(Teclado.Id, 1), new(Teclado.Id, 2)]);

        var pedido = await _handler.HandleAsync(comando, TestContext.Current.CancellationToken);

        pedido.Itens.Count.ShouldBe(2);
        pedido.Total.ShouldBe(450m);
        await _catalogo.Received(1).ObterPorIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(Teclado.Id)), Arg.Any<CancellationToken>());
    }
}

/// <summary>ISP: a consulta depende de uma interface de 1 método — o fake escrito à mão tem 1 método.</summary>
public class DesignResumoDeComprasTests
{
    private sealed class LeitorEmMemoria(params Pedido[] pedidos) : ILeitorDePedidos
    {
        public Task<IReadOnlyList<Pedido>> ListarDoClienteAsync(Guid clienteId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Pedido>>([.. pedidos.Where(p => p.ClienteId == clienteId)]);
    }

    [Fact]
    public async Task TotalGasto_SomaPedidosDoClienteIgnorandoCancelados()
    {
        var cliente = Guid.NewGuid();
        var produto = new Produto(Guid.NewGuid(), "Teclado", 100m, true);
        var pago = Pedido.Criar(cliente, [new(produto, 1)], SemDesconto.Instancia);
        var outro = Pedido.Criar(cliente, [new(produto, 2)], new DescontoFixo("F", 30m));
        var cancelado = Pedido.Criar(cliente, [new(produto, 5)], SemDesconto.Instancia);
        cancelado.Cancelar();
        var deOutroCliente = Pedido.Criar(Guid.NewGuid(), [new(produto, 9)], SemDesconto.Instancia);

        var resumo = new ResumoDeComprasDoCliente(new LeitorEmMemoria(pago, outro, cancelado, deOutroCliente));

        (await resumo.TotalGastoAsync(cliente, TestContext.Current.CancellationToken)).ShouldBe(270m);
    }
}

/// <summary>Os adaptadores traduzem as portas para a infraestrutura concreta, mantendo o formato do legado.</summary>
public class DesignAdaptadoresTests
{
    private static Pedido NovoPedido(Guid clienteId) =>
        Pedido.Criar(clienteId, [new(new Produto(Guid.NewGuid(), "Teclado", 150m, true), 1)], SemDesconto.Instancia);

    [Fact]
    public async Task PublicadorKafka_PublicaNoTopicoComChaveEJsonDoPedido()
    {
        var kafka = new ProdutorKafka();
        var pedido = NovoPedido(Guid.NewGuid());

        await new PublicadorKafkaDePedidos(kafka).PublishOrderCreatedAsync(pedido, TestContext.Current.CancellationToken);

        var mensagem = kafka.Mensagens.ShouldHaveSingleItem();
        mensagem.Topico.ShouldBe(PublicadorKafkaDePedidos.Topico);
        mensagem.Chave.ShouldBe(pedido.Id.ToString());
        using var json = JsonDocument.Parse(mensagem.Valor);
        json.RootElement.GetProperty("Total").GetDecimal().ShouldBe(150m);
    }

    [Fact]
    public async Task RepositorioSql_SalvaELista_PorCliente()
    {
        var banco = new BancoDeDadosSql();
        var repositorio = new RepositorioSqlDePedidos(banco);
        var cliente = Guid.NewGuid();
        var meu = NovoPedido(cliente);

        await repositorio.SalvarAsync(meu, TestContext.Current.CancellationToken);
        await repositorio.SalvarAsync(NovoPedido(Guid.NewGuid()), TestContext.Current.CancellationToken);

        banco.Pedidos.Count.ShouldBe(2);
        (await repositorio.ListarDoClienteAsync(cliente, TestContext.Current.CancellationToken)).ShouldBe([meu]);
    }

    [Fact]
    public async Task CatalogoSql_DevolveSoOsProdutosExistentes()
    {
        var banco = new BancoDeDadosSql();
        var teclado = new Produto(Guid.NewGuid(), "Teclado", 150m, true);
        banco.InserirProduto(teclado);

        var produtos = await new CatalogoSqlDeProdutos(banco).ObterPorIdsAsync([teclado.Id, Guid.NewGuid()], TestContext.Current.CancellationToken);

        produtos.ShouldBe([teclado]);
    }
}
