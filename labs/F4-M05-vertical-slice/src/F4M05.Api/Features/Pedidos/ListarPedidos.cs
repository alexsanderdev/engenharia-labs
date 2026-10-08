using F4M05.Api.Comum;
using F4M05.Api.Infraestrutura;

namespace F4M05.Api.Features.Pedidos;

/// <summary>
/// Fatia "Listar pedidos" (query). Devolve um resumo enxuto, sem itens: a query
/// projeta exatamente o que a tela precisa (num banco real: Select direto ou Dapper, sem carregar o agregado).
/// </summary>
public static class ListarPedidos
{
    public sealed record Query(Guid? ClienteId);

    public sealed record Resumo(Guid Id, Guid ClienteId, string Status, decimal Total);

    internal sealed class Handler(BancoEmMemoria db) : IQueryHandler<Query, IReadOnlyList<Resumo>>
    {
        public Task<IReadOnlyList<Resumo>> HandleAsync(Query query, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Resumo>>(
                [.. db.ListarPedidos(query.ClienteId).Select(p => new Resumo(p.Id, p.ClienteId, p.Status.ToString(), p.Total))]);
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapGet("/pedidos", async (Guid? clienteId, IQueryHandler<Query, IReadOnlyList<Resumo>> handler, CancellationToken ct) =>
                Results.Ok(await handler.HandleAsync(new Query(clienteId), ct)));
    }
}
