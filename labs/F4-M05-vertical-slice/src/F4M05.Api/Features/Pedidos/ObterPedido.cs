using F4M05.Api.Comum;
using F4M05.Api.Infraestrutura;

namespace F4M05.Api.Features.Pedidos;

/// <summary>Fatia "Obter pedido" (query): leitura pura, sem validator, sem decorator.</summary>
public static class ObterPedido
{
    public sealed record Query(Guid Id);

    // Sim, parecido com CriarPedido.Response. Duplicação DELIBERADA: as duas telas evoluem separadas.
    public sealed record Response(Guid Id, Guid ClienteId, string Status, decimal Total, List<ItemResponse> Itens);

    public sealed record ItemResponse(Guid ProdutoId, string Nome, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

    internal sealed class Handler(BancoEmMemoria db) : IQueryHandler<Query, Response>
    {
        public Task<Response> HandleAsync(Query query, CancellationToken ct)
        {
            var pedido = db.ObterPedido(query.Id) ?? throw new NaoEncontradoException("Pedido", query.Id);

            return Task.FromResult(new Response(
                pedido.Id,
                pedido.ClienteId,
                pedido.Status.ToString(),
                pedido.Total,
                [.. pedido.Itens.Select(i => new ItemResponse(i.ProdutoId, i.Nome, i.Quantidade, i.PrecoUnitario, i.Subtotal))]));
        }
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapGet("/pedidos/{id:guid}", async (Guid id, IQueryHandler<Query, Response> handler, CancellationToken ct) =>
                Results.Ok(await handler.HandleAsync(new Query(id), ct)));
    }
}
