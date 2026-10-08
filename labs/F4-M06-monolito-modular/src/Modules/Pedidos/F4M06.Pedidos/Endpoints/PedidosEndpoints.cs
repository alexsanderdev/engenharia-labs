using F4M06.Catalogo.Infra;
using F4M06.Clientes.Contracts;
using F4M06.Pedidos.Dominio;
using F4M06.Pedidos.Infra;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace F4M06.Pedidos.Endpoints;

public sealed record ItemRequest(Guid ProdutoId, int Quantidade);

public sealed record CriarPedidoRequest(Guid ClienteId, IReadOnlyList<ItemRequest>? Itens);

public sealed record ItemPedidoResponse(Guid ProdutoId, string NomeProduto, decimal PrecoUnitario, int Quantidade);

public sealed record PedidoResponse(Guid Id, Guid ClienteId, string Status, decimal Total, IReadOnlyList<ItemPedidoResponse> Itens)
{
    public static PedidoResponse De(Pedido p) => new(
        p.Id, p.ClienteId, p.Status.ToString(), p.Total,
        [.. p.Itens.Select(i => new ItemPedidoResponse(i.ProdutoId, i.NomeProduto, i.PrecoUnitario, i.Quantidade))]);
}

/// <summary>API HTTP de Pedidos (rotas sob <c>/pedidos</c>).</summary>
public static class PedidosEndpoints
{
    public static void MapPedidosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/pedidos");
        grupo.MapPost("/", CriarAsync);
        grupo.MapGet("/{id:guid}", ObterAsync);
        grupo.MapPost("/{id:guid}/confirmar", ConfirmarAsync);
    }

    /// <summary>
    /// Cria o pedido. Com Clientes, a conversa já é pelo contrato público (<see cref="IClientesApi"/>).
    /// Com o Catálogo... "era só uma consulta rápida": lê direto o DbContext do outro módulo.
    /// </summary>
    private static async Task<IResult> CriarAsync(
        CriarPedidoRequest request,
        PedidosDbContext db,
        CatalogoDbContext catalogo,
        IClientesApi clientes,
        TimeProvider relogio,
        CancellationToken ct)
    {
        if (request.Itens is not { Count: > 0 } itens)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["itens"] = ["O pedido precisa de ao menos um item."] });
        if (itens.Any(i => i.Quantidade <= 0))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["itens"] = ["Quantidade deve ser maior que zero."] });

        if (!await clientes.ExisteAsync(request.ClienteId, ct))
            return Results.Problem($"Cliente {request.ClienteId} não encontrado.", statusCode: StatusCodes.Status422UnprocessableEntity);

        // Acesso direto às tabelas e à entidade Produto do Catálogo.
        var ids = itens.Select(i => i.ProdutoId).Distinct().ToArray();
        var produtos = await catalogo.Produtos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var pedido = new Pedido(Guid.NewGuid(), request.ClienteId, relogio.GetUtcNow());
        foreach (var item in itens)
        {
            if (!produtos.TryGetValue(item.ProdutoId, out var produto))
                return Results.Problem($"Produto {item.ProdutoId} não existe no catálogo.", statusCode: StatusCodes.Status422UnprocessableEntity);
            if (!produto.Ativo)
                return Results.Problem($"Produto '{produto.Nome}' está inativo e não pode entrar em pedido.", statusCode: StatusCodes.Status422UnprocessableEntity);

            pedido.AdicionarItem(produto.Id, produto.Nome, produto.Preco, item.Quantidade);
        }

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/pedidos/{pedido.Id}", PedidoResponse.De(pedido));
    }

    private static async Task<IResult> ObterAsync(Guid id, PedidosDbContext db, CancellationToken ct) =>
        await db.Pedidos.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct) is { } pedido
            ? Results.Ok(PedidoResponse.De(pedido))
            : Results.NotFound();

    /// <summary>Confirma o pedido.</summary>
    private static async Task<IResult> ConfirmarAsync(Guid id, PedidosDbContext db, CancellationToken ct)
    {
        if (await db.Pedidos.SingleOrDefaultAsync(p => p.Id == id, ct) is not { } pedido) return Results.NotFound();

        try
        {
            pedido.Confirmar();
        }
        catch (TransicaoInvalidaException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }

        await db.SaveChangesAsync(ct);

        // TODO (Passo 5): depois do commit, publicar o evento de integração PedidoConfirmado
        // (F4M06.Pedidos.Contracts) pelo IEventBus. Pedidos não deve saber quem vai reagir.

        return Results.Ok(PedidoResponse.De(pedido));
    }
}
