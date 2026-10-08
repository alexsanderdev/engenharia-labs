using F4M05.Api.Comum;
using F4M05.Api.Infraestrutura;
using FluentValidation;

namespace F4M05.Api.Features.Pedidos;

/// <summary>Fatia "Confirmar pedido" (command). A regra de transição está no domínio (<c>Pedido.Confirmar</c>).</summary>
public static class ConfirmarPedido
{
    public sealed record Command(Guid Id);

    public sealed record Response(Guid Id, string Status);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(c => c.Id).NotEmpty();
    }

    internal sealed class Handler(BancoEmMemoria db) : ICommandHandler<Command, Response>
    {
        public async Task<Response> HandleAsync(Command command, CancellationToken ct)
        {
            var pedido = db.ObterPedido(command.Id) ?? throw new NaoEncontradoException("Pedido", command.Id);

            pedido.Confirmar();
            await db.SalvarAsync(ct);

            return new Response(pedido.Id, pedido.Status.ToString());
        }
    }

    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost("/pedidos/{id:guid}/confirmar", async (Guid id, ICommandHandler<Command, Response> handler, CancellationToken ct) =>
                Results.Ok(await handler.HandleAsync(new Command(id), ct)));
    }
}
