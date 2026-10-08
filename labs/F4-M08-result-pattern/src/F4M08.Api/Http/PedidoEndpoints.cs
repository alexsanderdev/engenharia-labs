using F4M08.Api.Pedidos;
using F4M08.Api.Resultados;
using Microsoft.AspNetCore.Mvc;

namespace F4M08.Api.Http;

/// <summary>
/// Endpoints (PRONTOS). Repare que nenhum deles tem try/catch nem if por tipo de erro:
/// cada um faz Match do Result — sucesso vira o status de sucesso, falha vira ProblemDetails.
/// </summary>
public static class PedidoEndpoints
{
    public const string HeaderCliente = "X-Cliente-Id";

    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var pedidos = app.MapGroup("/pedidos").WithTags("Pedidos");

        pedidos.MapPost("/", async (CriarPedidoRequest request, PedidosCasosDeUso casos, CancellationToken ct) =>
            (await casos.CriarAsync(request, ct)).Match<IResult>(
                p => TypedResults.Created($"/pedidos/{p.Id}", p),
                e => e.ToProblem()));

        pedidos.MapGet("/{id:guid}", async (Guid id, PedidosCasosDeUso casos, CancellationToken ct) =>
            (await casos.ObterAsync(id, ct)).Match<IResult>(TypedResults.Ok, e => e.ToProblem()));

        pedidos.MapPost("/{id:guid}/confirmar", async (Guid id, PedidosCasosDeUso casos, CancellationToken ct) =>
            (await casos.ConfirmarAsync(id, ct)).Match<IResult>(TypedResults.Ok, e => e.ToProblem()));

        pedidos.MapPost("/{id:guid}/concluir", async (Guid id, PedidosCasosDeUso casos, CancellationToken ct) =>
            (await casos.ConcluirAsync(id, ct)).Match<IResult>(TypedResults.Ok, e => e.ToProblem()));

        // Quem está pedindo vem de um header só para simplificar o lab; na vida real, do token (Fase 5).
        pedidos.MapPost("/{id:guid}/cancelar", async (
                Guid id,
                [FromHeader(Name = HeaderCliente)] Guid clienteId,
                PedidosCasosDeUso casos,
                CancellationToken ct) =>
            (await casos.CancelarAsync(id, clienteId, ct)).Match<IResult>(TypedResults.NoContent, e => e.ToProblem()));

        return app;
    }
}
