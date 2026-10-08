using F5M03.Api.Dominio;

namespace F5M03.Api.Pedidos;

// VULNERÁVEL — corrija (Passos 1, 2 e 3 do Lab).
public static class PedidoEndpoints
{
    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/pedidos");

        // VULNERÁVEL: o corpo é desserializado direto na entidade. Total, Status, CustoTotal,
        // ObservacaoInterna e o PrecoUnitario de cada item podem vir do cliente.
        grupo.MapPost("/", (Pedido pedido, IProdutoRepositorio produtos, IPedidoRepositorio pedidos, TimeProvider relogio) =>
        {
            pedido.Id = Guid.NewGuid();
            pedido.CriadoEm = relogio.GetUtcNow();
            pedido.ObservacaoInterna ??= "Pedido criado pela API pública.";

            foreach (var item in pedido.Itens)
            {
                if (produtos.Obter(item.ProdutoId) is not { } produto) return Results.BadRequest();

                // "O app mobile manda o preço da promoção" — e qualquer um pode mandar o preço que quiser.
                if (item.PrecoUnitario <= 0) item.PrecoUnitario = produto.Preco;
                pedido.CustoTotal += produto.CustoInterno * item.Quantidade;
            }

            if (pedido.Total <= 0) pedido.Total = pedido.Itens.Sum(i => i.PrecoUnitario * i.Quantidade);

            pedidos.Adicionar(pedido);
            // VULNERÁVEL: devolve a entidade (CustoTotal, ObservacaoInterna).
            return Results.Created($"/pedidos/{pedido.Id}", pedido);
        });

        grupo.MapGet("/{id:guid}", (Guid id, IPedidoRepositorio pedidos) =>
            pedidos.Obter(id) is { } pedido ? Results.Ok(pedido) : Results.NotFound());

        return app;
    }
}
