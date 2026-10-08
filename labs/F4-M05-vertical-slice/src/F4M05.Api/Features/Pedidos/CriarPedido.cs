using F4M05.Api.Comum;
using F4M05.Api.Dominio;
using F4M05.Api.Infraestrutura;
using FluentValidation;

namespace F4M05.Api.Features.Pedidos;

/// <summary>
/// Fatia "Criar pedido": entrada, validação, handler, saída e rota NO MESMO ARQUIVO.
/// Para mudar como um pedido é criado, você abre este arquivo — e só ele.
/// </summary>
public static class CriarPedido
{
    // ---------- Entrada ----------
    public sealed record Command(Guid ClienteId, List<ItemCommand> Itens);

    public sealed record ItemCommand(Guid ProdutoId, int Quantidade);

    // ---------- Saída (DTO da fatia; não é a entidade) ----------
    public sealed record Response(Guid Id, Guid ClienteId, string Status, decimal Total, List<ItemResponse> Itens);

    public sealed record ItemResponse(Guid ProdutoId, string Nome, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

    // ---------- Validação de ENTRADA (forma do pedido). Regra de negócio fica no domínio. ----------
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ClienteId).NotEmpty();
            RuleFor(c => c.Itens).NotEmpty().WithMessage("O pedido precisa ter pelo menos um item.");
            RuleForEach(c => c.Itens).ChildRules(item =>
            {
                item.RuleFor(i => i.ProdutoId).NotEmpty();
                item.RuleFor(i => i.Quantidade).GreaterThan(0);
            });
        }
    }

    // ---------- Caso de uso ----------
    internal sealed class Handler(BancoEmMemoria db) : ICommandHandler<Command, Response>
    {
        public async Task<Response> HandleAsync(Command command, CancellationToken ct)
        {
            var produtos = db.ObterProdutos(command.Itens.Select(i => i.ProdutoId)).ToDictionary(p => p.Id);

            var linhas = command.Itens.Select(i => produtos.TryGetValue(i.ProdutoId, out var produto)
                ? (produto, i.Quantidade)
                : throw new NaoEncontradoException("Produto", i.ProdutoId));

            var pedido = Pedido.Criar(command.ClienteId, linhas);

            db.AdicionarPedido(pedido);
            await db.SalvarAsync(ct);

            return new Response(
                pedido.Id,
                pedido.ClienteId,
                pedido.Status.ToString(),
                pedido.Total,
                [.. pedido.Itens.Select(i => new ItemResponse(i.ProdutoId, i.Nome, i.Quantidade, i.PrecoUnitario, i.Subtotal))]);
        }
    }

    // ---------- Rota ----------
    internal sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost("/pedidos", async (Command command, ICommandHandler<Command, Response> handler, CancellationToken ct) =>
            {
                var response = await handler.HandleAsync(command, ct);
                return Results.Created($"/pedidos/{response.Id}", response);
            });
    }
}
