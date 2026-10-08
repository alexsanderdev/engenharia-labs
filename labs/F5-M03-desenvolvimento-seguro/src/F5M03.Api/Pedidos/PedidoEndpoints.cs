using F5M03.Api.Dominio;
using Microsoft.AspNetCore.Http.HttpResults;

namespace F5M03.Api.Pedidos;

public static class PedidoEndpoints
{
    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/pedidos");
        grupo.MapPost("/", Criar);
        grupo.MapGet("/{id:guid}", Obter);
        return app;
    }

    internal static Results<Created<PedidoResponse>, ValidationProblem> Criar(
        CriarPedidoRequest requisicao, IProdutoRepositorio produtos, IPedidoRepositorio pedidos, TimeProvider relogio)
    {
        var erros = ValidadorDePedido.Validar(requisicao, produtos);
        if (!erros.Vazio) return TypedResults.ValidationProblem(erros.ParaDicionario());

        var pedido = new Pedido
        {
            Id = Guid.NewGuid(),
            ClienteId = requisicao.ClienteId,
            Status = StatusPedido.Created,
            CriadoEm = relogio.GetUtcNow(),
            ObservacaoInterna = "Pedido criado pela API pública.",
        };

        foreach (var item in requisicao.Itens!)
        {
            var produto = produtos.Obter(item.ProdutoId)!;
            // Preço SEMPRE do catálogo, nunca do corpo da requisição.
            pedido.Itens.Add(new ItemPedido { ProdutoId = produto.Id, Quantidade = item.Quantidade, PrecoUnitario = produto.Preco });
            pedido.CustoTotal += produto.CustoInterno * item.Quantidade;
        }
        pedido.Total = pedido.Itens.Sum(i => i.PrecoUnitario * i.Quantidade);

        pedidos.Adicionar(pedido);
        return TypedResults.Created($"/pedidos/{pedido.Id}", ParaResposta(pedido));
    }

    internal static Results<Ok<PedidoResponse>, NotFound> Obter(Guid id, IPedidoRepositorio pedidos) =>
        pedidos.Obter(id) is { } pedido ? TypedResults.Ok(ParaResposta(pedido)) : TypedResults.NotFound();

    private static PedidoResponse ParaResposta(Pedido p) =>
        new(p.Id, p.ClienteId,
            [.. p.Itens.Select(i => new ItemPedidoResponse(i.ProdutoId, i.Quantidade, i.PrecoUnitario))],
            p.Total, p.Status, p.CriadoEm);
}
