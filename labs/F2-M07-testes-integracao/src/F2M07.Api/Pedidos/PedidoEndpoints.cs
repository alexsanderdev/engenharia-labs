using System.Security.Claims;
using F2M07.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace F2M07.Api.Pedidos;

public static class PedidoEndpoints
{
    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pedidos").RequireAuthorization();
        group.MapGet("/", ListarDoCliente);
        group.MapGet("/{id:guid}", ObterPorId);
        group.MapPost("/", Criar);
        group.MapPost("/{id:guid}/cancelar", Cancelar);
        return app;
    }

    public static async Task<Ok<PedidoResponse[]>> ListarDoCliente(
        ClaimsPrincipal user, PedidosDbContext db, CancellationToken ct)
    {
        var clienteId = user.ObterClienteId();
        var pedidos = await db.Pedidos.AsNoTracking()
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.CriadoEm)
            .ToListAsync(ct);
        return TypedResults.Ok(pedidos.Select(PedidoResponse.De).ToArray());
    }

    /// <summary>Pedido de outro cliente devolve 404 (não 403): não revelamos que o id existe.</summary>
    public static async Task<Results<Ok<PedidoResponse>, NotFound>> ObterPorId(
        Guid id, ClaimsPrincipal user, PedidosDbContext db, CancellationToken ct)
    {
        var clienteId = user.ObterClienteId();
        var pedido = await db.Pedidos.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id && p.ClienteId == clienteId, ct);
        return pedido is null ? TypedResults.NotFound() : TypedResults.Ok(PedidoResponse.De(pedido));
    }

    /// <summary>
    /// Cria o pedido do cliente autenticado.
    /// 400 (ValidationProblem) para formato inválido; 422 (ProblemDetails) para produto inexistente
    /// ou inativo; 201 com Location e o pedido com total calculado no servidor.
    /// </summary>
    public static async Task<Results<Created<PedidoResponse>, ValidationProblem, ProblemHttpResult>> Criar(
        CriarPedidoRequest request,
        ClaimsPrincipal user,
        PedidosDbContext db,
        TimeProvider relogio,
        CancellationToken ct)
    {
        var erros = PedidoValidator.Validar(request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var ids = request.Itens!.Select(i => i.ProdutoId).Distinct().ToArray();
        var produtos = await db.Produtos
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var indisponiveis = ids
            .Where(id => !produtos.TryGetValue(id, out var produto) || !produto.Ativo)
            .ToArray();
        if (indisponiveis.Length > 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Produto indisponível",
                detail: $"Produtos inexistentes ou inativos: {string.Join(", ", indisponiveis)}.");
        }

        var pedido = new Pedido(Guid.NewGuid(), user.ObterClienteId(), relogio.GetUtcNow());
        foreach (var item in request.Itens!)
            pedido.AdicionarItem(produtos[item.ProdutoId], item.Quantidade);

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/pedidos/{pedido.Id}", PedidoResponse.De(pedido));
    }

    /// <summary>204 se cancelou; 404 se não existe (ou é de outro cliente); 409 se o status não permite.</summary>
    public static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Cancelar(
        Guid id, ClaimsPrincipal user, PedidosDbContext db, CancellationToken ct)
    {
        var clienteId = user.ObterClienteId();
        var pedido = await db.Pedidos.SingleOrDefaultAsync(p => p.Id == id && p.ClienteId == clienteId, ct);
        if (pedido is null) return TypedResults.NotFound();

        if (!pedido.PodeCancelar)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Pedido não pode ser cancelado",
                detail: $"Pedidos com status {pedido.Status} não podem ser cancelados.");
        }

        pedido.Cancelar();
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
