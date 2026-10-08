using F4M01.Application.Pedidos;

namespace F4M01.Api.Endpoints;

/// <summary>Contrato HTTP de entrada. Sem preço: o cliente não decide quanto paga.</summary>
public sealed record CriarPedidoRequest(Guid ClienteId, List<CriarPedidoItemRequest> Itens);

public sealed record CriarPedidoItemRequest(Guid ProdutoId, int Quantidade);

/// <summary>
/// Endpoints "magros": traduzem HTTP → comando, chamam o caso de uso e traduzem o DTO → HTTP.
/// Nenhuma regra de negócio, nenhum DbContext.
/// </summary>
public static class PedidoEndpoints
{
    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/pedidos", async (CriarPedidoRequest request, CriarPedidoHandler handler, CancellationToken ct) =>
        {
            var command = new CriarPedidoCommand(
                request.ClienteId,
                [.. request.Itens.Select(i => new ItemDoPedidoCommand(i.ProdutoId, i.Quantidade))]);

            var criado = await handler.HandleAsync(command, ct);
            return Results.Created($"/pedidos/{criado.Id}", criado);
        });

        app.MapGet("/clientes/{clienteId:guid}/pedidos", async (Guid clienteId, ListarPedidosDoClienteHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(clienteId, ct)));

        return app;
    }
}
