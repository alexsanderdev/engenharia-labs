using System.Text.Json;

using F2M02.Solid.Dominio;
using F2M02.Solid.Infraestrutura;
using F2M02.Solid.Legado;

namespace F2M02.Solid.Tests;

/// <summary>
/// Testes de CARACTERIZAÇÃO do PedidoService legado: o que ele faz hoje com banco, SMTP e Kafka.
/// Passam desde o início e precisam continuar verdes em TODOS os passos da refatoração.
/// </summary>
public class ComportamentoPedidoServiceTests
{
    private static readonly Guid ClienteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Produto Teclado = new(Guid.NewGuid(), "Teclado", 150m, Ativo: true);
    private static readonly Produto Mouse = new(Guid.NewGuid(), "Mouse", 50m, Ativo: true);
    private static readonly Produto Descontinuado = new(Guid.NewGuid(), "Monitor CRT", 300m, Ativo: false);

    private readonly BancoDeDadosSql _banco = new();
    private readonly ServidorSmtp _smtp = new();
    private readonly ProdutorKafka _kafka = new();
    private readonly PedidoService _service;

    public ComportamentoPedidoServiceTests()
    {
        _banco.InserirProduto(Teclado);
        _banco.InserirProduto(Mouse);
        _banco.InserirProduto(Descontinuado);
        _service = new PedidoService(_banco, _smtp, _kafka);
    }

    [Fact]
    public void CriarPedido_Valido_CalculaTotalESalvaNoBanco()
    {
        var pedido = _service.CriarPedido(ClienteId, "ana@exemplo.com", [(Teclado.Id, 1), (Mouse.Id, 2)], null);

        pedido.Subtotal.ShouldBe(250m);
        pedido.Desconto.ShouldBe(0m);
        pedido.Total.ShouldBe(250m);
        pedido.Status.ShouldBe(StatusPedido.Created);
        pedido.ClienteId.ShouldBe(ClienteId);
        pedido.Itens.Count.ShouldBe(2);
        _banco.Pedidos.ShouldHaveSingleItem().ShouldBeSameAs(pedido);
    }

    [Fact]
    public void CriarPedido_Valido_EnviaEmailDeConfirmacao()
    {
        var pedido = _service.CriarPedido(ClienteId, "ana@exemplo.com", [(Teclado.Id, 1)], null);

        var email = _smtp.Enviados.ShouldHaveSingleItem();
        email.Para.ShouldBe("ana@exemplo.com");
        email.Assunto.ShouldBe("Pedido recebido");
        email.Corpo.ShouldBe($"Olá! Seu pedido {pedido.Id} foi criado. Total: R$ 150.00");
    }

    [Fact]
    public void CriarPedido_Valido_PublicaEventoNoKafka()
    {
        var pedido = _service.CriarPedido(ClienteId, "ana@exemplo.com", [(Teclado.Id, 2)], null);

        var mensagem = _kafka.Mensagens.ShouldHaveSingleItem();
        mensagem.Topico.ShouldBe("pedidos.criados");
        mensagem.Chave.ShouldBe(pedido.Id.ToString());
        using var json = JsonDocument.Parse(mensagem.Valor);
        json.RootElement.GetProperty("Id").GetGuid().ShouldBe(pedido.Id);
        json.RootElement.GetProperty("ClienteId").GetGuid().ShouldBe(ClienteId);
        json.RootElement.GetProperty("Total").GetDecimal().ShouldBe(300m);
    }

    [Theory]
    [InlineData("BLACKFRIDAY", 3, 90)]      // 20% de 450
    [InlineData("blackfriday", 1, 30)]      // não diferencia maiúsculas
    [InlineData("PRIMEIRACOMPRA", 1, 15)]   // 10% de 150
    [InlineData("PRIMEIRACOMPRA", 5, 50)]   // 10% de 750 = 75, mas o teto é 50
    [InlineData("BEMVINDO30", 1, 30)]
    [InlineData("  ", 1, 0)]                // cupom em branco = sem desconto
    public void CriarPedido_ComCupom_AplicaODescontoDoCupom(string cupom, int quantidadeDeTeclados, int descontoEsperado)
    {
        var pedido = _service.CriarPedido(ClienteId, "ana@exemplo.com", [(Teclado.Id, quantidadeDeTeclados)], cupom);

        pedido.Desconto.ShouldBe(descontoEsperado);
    }

    [Fact]
    public void CriarPedido_CupomFixoMaiorQueOPedido_DescontaNoMaximoOSubtotal()
    {
        _banco.InserirProduto(new Produto(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Adesivo", 20m, true));

        var pedido = _service.CriarPedido(ClienteId, "ana@exemplo.com", [(Guid.Parse("22222222-2222-2222-2222-222222222222"), 1)], "BEMVINDO30");

        pedido.Desconto.ShouldBe(20m);
        pedido.Total.ShouldBe(0m);
    }

    [Fact]
    public void CriarPedido_CupomInvalido_LancaENaoTemEfeitos()
    {
        var ex = Should.Throw<PedidoInvalidoException>(() =>
            _service.CriarPedido(ClienteId, "ana@exemplo.com", [(Teclado.Id, 1)], "NATAL"));

        ex.Message.ShouldBe("Cupom inválido: NATAL");
        _banco.Pedidos.ShouldBeEmpty();
        _smtp.Enviados.ShouldBeEmpty();
        _kafka.Mensagens.ShouldBeEmpty();
    }

    [Fact]
    public void CriarPedido_ProdutoInativo_LancaENaoTemEfeitos()
    {
        var ex = Should.Throw<PedidoInvalidoException>(() =>
            _service.CriarPedido(ClienteId, "ana@exemplo.com", [(Teclado.Id, 1), (Descontinuado.Id, 1)], null));

        ex.Message.ShouldBe("Produto inativo: Monitor CRT");
        _banco.Pedidos.ShouldBeEmpty();
        _kafka.Mensagens.ShouldBeEmpty();
    }

    [Fact]
    public void CriarPedido_ProdutoInexistente_Lanca()
    {
        var id = Guid.Parse("99999999-9999-9999-9999-999999999999");

        Should.Throw<PedidoInvalidoException>(() => _service.CriarPedido(ClienteId, "ana@exemplo.com", [(id, 1)], null))
            .Message.ShouldBe($"Produto não encontrado: {id}");
    }

    [Fact]
    public void CriarPedido_SemItens_Lanca()
    {
        Should.Throw<PedidoInvalidoException>(() => _service.CriarPedido(ClienteId, "ana@exemplo.com", [], null))
            .Message.ShouldBe("Pedido precisa de ao menos um item");
    }

    [Fact]
    public void CriarPedido_QuantidadeZero_Lanca()
    {
        Should.Throw<PedidoInvalidoException>(() => _service.CriarPedido(ClienteId, "ana@exemplo.com", [(Teclado.Id, 0)], null))
            .Message.ShouldBe("Quantidade deve ser maior que zero");
    }

    [Fact]
    public void CriarPedido_BancoFora_NaoEnviaEmailNemEvento()
    {
        _banco.SimularFalha = true;

        Should.Throw<InvalidOperationException>(() => _service.CriarPedido(ClienteId, "ana@exemplo.com", [(Teclado.Id, 1)], null));

        _smtp.Enviados.ShouldBeEmpty();
        _kafka.Mensagens.ShouldBeEmpty();
    }
}
