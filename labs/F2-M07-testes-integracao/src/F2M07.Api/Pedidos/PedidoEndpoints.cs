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
    public static Task<Results<Created<PedidoResponse>, ValidationProblem, ProblemHttpResult>> Criar(
        CriarPedidoRequest request,
        ClaimsPrincipal user,
        PedidosDbContext db,
        TimeProvider relogio,
        CancellationToken ct) =>
        throw new NotImplementedException(
            "TODO (Passo 6): valide com PedidoValidator (400); carregue os produtos dos itens numa única " +
            "consulta; se algum não existir ou estiver inativo devolva TypedResults.Problem com 422; senão " +
            "crie o Pedido (cliente = user.ObterClienteId(), data = relogio.GetUtcNow()), adicione os itens, " +
            "salve e devolva TypedResults.Created(\"/pedidos/{id}\", PedidoResponse.De(pedido)).");

    /// <summary>204 se cancelou; 404 se não existe (ou é de outro cliente); 409 se o status não permite.</summary>
    public static Task<Results<NoContent, NotFound, ProblemHttpResult>> Cancelar(
        Guid id, ClaimsPrincipal user, PedidosDbContext db, CancellationToken ct) =>
        throw new NotImplementedException(
            "TODO (Passo 6): busque o pedido do cliente (404 se não achar); se !pedido.PodeCancelar devolva " +
            "TypedResults.Problem com 409; senão chame pedido.Cancelar(), salve e devolva 204.");
}
