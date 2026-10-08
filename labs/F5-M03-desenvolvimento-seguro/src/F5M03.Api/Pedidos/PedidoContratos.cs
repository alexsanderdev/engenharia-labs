using F5M03.Api.Dominio;
using F5M03.Api.Validacao;

namespace F5M03.Api.Pedidos;

/// <summary>Entrada: o cliente escolhe O QUE comprar e QUANTO. Preço, total e status são do servidor.</summary>
public sealed record CriarPedidoRequest(Guid ClienteId, IReadOnlyList<ItemPedidoRequest>? Itens);

public sealed record ItemPedidoRequest(Guid ProdutoId, int Quantidade);

/// <summary>Saída: sem CustoTotal nem ObservacaoInterna.</summary>
public sealed record PedidoResponse(
    Guid Id, Guid ClienteId, IReadOnlyList<ItemPedidoResponse> Itens, decimal Total, StatusPedido Status, DateTimeOffset CriadoEm);

public sealed record ItemPedidoResponse(Guid ProdutoId, int Quantidade, decimal PrecoUnitario);

public static class ValidadorDePedido
{
    public const int MaximoDeItens = 20;
    public const int QuantidadeMinima = 1;
    public const int QuantidadeMaxima = 100;

    public static ErrosDeValidacao Validar(CriarPedidoRequest r, IProdutoRepositorio produtos)
    {
        var erros = new ErrosDeValidacao();
        erros.Se(r.ClienteId == Guid.Empty, nameof(r.ClienteId), "ClienteId é obrigatório.");

        if (r.Itens is null || r.Itens.Count is 0 or > MaximoDeItens)
        {
            erros.Adicionar(nameof(r.Itens), $"O pedido deve ter de 1 a {MaximoDeItens} itens.");
            return erros;
        }

        for (var i = 0; i < r.Itens.Count; i++)
        {
            var item = r.Itens[i];
            erros.Se(item.Quantidade is < QuantidadeMinima or > QuantidadeMaxima, $"Itens[{i}].Quantidade",
                $"Quantidade deve estar entre {QuantidadeMinima} e {QuantidadeMaxima}.");
            erros.Se(produtos.Obter(item.ProdutoId) is not { Ativo: true }, $"Itens[{i}].ProdutoId",
                "Produto inexistente ou inativo.");
        }

        return erros;
    }
}
