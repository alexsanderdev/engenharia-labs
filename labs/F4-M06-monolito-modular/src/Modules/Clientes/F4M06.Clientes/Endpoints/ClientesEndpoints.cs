using F4M06.Clientes.Dominio;
using F4M06.Clientes.Infra;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace F4M06.Clientes.Endpoints;

internal sealed record CriarClienteRequest(string? Nome, string? Email);

internal sealed record ClienteResponse(Guid Id, string Nome, string Email, decimal TotalGasto, int PedidosConfirmados, string Nivel)
{
    public static ClienteResponse De(Cliente c) =>
        new(c.Id, c.Nome, c.Email, c.TotalGasto, c.PedidosConfirmados, c.Nivel.ToString());
}

/// <summary>API HTTP de Clientes (rotas sob <c>/clientes</c>).</summary>
internal static class ClientesEndpoints
{
    public static void MapClientesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/clientes");

        grupo.MapPost("/", async (CriarClienteRequest request, ClientesDbContext db, CancellationToken ct) =>
        {
            var erros = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.Nome)) erros["nome"] = ["Nome é obrigatório."];
            if (string.IsNullOrWhiteSpace(request.Email)) erros["email"] = ["E-mail é obrigatório."];
            if (erros.Count > 0) return Results.ValidationProblem(erros);

            var cliente = new Cliente(Guid.NewGuid(), request.Nome!, request.Email!);
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/clientes/{cliente.Id}", ClienteResponse.De(cliente));
        });

        grupo.MapGet("/{id:guid}", async (Guid id, ClientesDbContext db, CancellationToken ct) =>
            await db.Clientes.FindAsync([id], ct) is { } cliente
                ? Results.Ok(ClienteResponse.De(cliente))
                : Results.NotFound());
    }
}
