namespace F4M02.Pedidos.Domain.Tests;

/// <summary>
/// Construtores de cenário (test data builders) na linguagem do domínio. Mantêm os testes curtos:
/// cada teste só diz o que é relevante para a regra que está descrevendo.
/// </summary>
internal static class Dado
{
    public static readonly DateTimeOffset Agora = new(2026, 10, 8, 14, 0, 0, TimeSpan.Zero);

    public static readonly ClienteId Cliente = new(Guid.Parse("0199b0a0-0000-7000-8000-000000000001"));

    public static EnderecoDeEntrega Endereco() => new("Av. Paulista", "1000", "São Paulo", "SP", "01310-100");

    public static ProdutoDoCatalogo Produto(string sku = "CAFE-500G", decimal preco = 10m, bool ativo = true, string moeda = "BRL", string nome = "Café 500 g") =>
        new(ProdutoId.Novo(), new Sku(sku), nome, new Dinheiro(preco, moeda), ativo);

    public static Pedido PedidoNovo(ClienteId? cliente = null) =>
        Pedido.Criar(cliente ?? Cliente, Endereco(), "BRL", Agora);

    /// <summary>Pedido em Created com um item de <paramref name="preco"/> × <paramref name="quantidade"/>.</summary>
    public static Pedido PedidoComItem(decimal preco = 10m, int quantidade = 1, ClienteId? cliente = null)
    {
        var pedido = PedidoNovo(cliente);
        pedido.AdicionarItem(Produto(preco: preco), new Quantidade(quantidade));
        return pedido;
    }

    /// <summary>Leva um pedido com item até o status pedido, pelos caminhos válidos.</summary>
    public static Pedido PedidoNoStatus(StatusPedido status)
    {
        var pedido = PedidoComItem();
        switch (status)
        {
            case StatusPedido.Confirmed:
                pedido.Confirmar(Agora);
                break;
            case StatusPedido.Completed:
                pedido.Confirmar(Agora);
                pedido.Concluir();
                break;
            case StatusPedido.Cancelled:
                pedido.Cancelar("Cliente desistiu", Agora);
                break;
        }

        return pedido;
    }

    /// <summary>Executa a ação e devolve o código da regra violada.</summary>
    public static string RegraVioladaPor(Action acao) =>
        Should.Throw<RegraDeNegocioVioladaException>(acao).Regra;
}
