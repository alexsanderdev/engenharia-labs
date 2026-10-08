using F5M05.Api.Autenticacao;
using F5M05.Api.Catalogo;
using F5M05.Api.Idempotencia;
using F5M05.Api.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;

namespace F5M05.Api.Pedidos;

public static class PedidoEndpoints
{
    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/pedidos").RequireAuthorization();

        grupo.MapPost("/", Criar)
            .RequireRateLimiting(PoliticasDeLimite.Pedidos)   // escrita: janela fixa por cliente
            .ExigirIdempotencia();                            // retry seguro: Idempotency-Key

        grupo.MapGet("/{id:guid}", Obter)
            .RequireRateLimiting(PoliticasDeLimite.Consultas); // leitura: janela deslizante por cliente

        return app;
    }

    internal static async Task<Results<Created<PedidoResponse>, ValidationProblem>> Criar(
        CriarPedidoRequest requisicao, HttpContext contexto, ICatalogo catalogo, IRepositorioDePedidos pedidos,
        TimeProvider relogio, CancellationToken ct)
    {
        if (requisicao.Itens is not { Count: > 0 } itens || itens.Any(i => i.Quantidade is < 1 or > 100))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["Itens"] = ["Informe de 1 a 100 unidades por item."] });

        var linhas = new List<ItemPedidoResponse>();
        foreach (var item in itens)
        {
            if (await catalogo.ObterAsync(item.ProdutoId, ct) is not { } produto)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["Itens"] = [$"Produto {item.ProdutoId} não existe."] });
            linhas.Add(new ItemPedidoResponse(produto.Id, item.Quantidade, produto.Preco));
        }

        var pedido = new Pedido(Guid.NewGuid(), contexto.User.ClienteId()!, linhas,
            linhas.Sum(l => l.PrecoUnitario * l.Quantidade), relogio.GetUtcNow());
        await pedidos.AdicionarAsync(pedido, ct);

        return TypedResults.Created($"/pedidos/{pedido.Id}", pedido.ParaResposta());
    }

    internal static async Task<Results<Ok<PedidoResponse>, NotFound>> Obter(
        Guid id, HttpContext contexto, IRepositorioDePedidos pedidos, CancellationToken ct) =>
        await pedidos.ObterAsync(id, ct) is { } pedido && pedido.ClienteId == contexto.User.ClienteId()
            ? TypedResults.Ok(pedido.ParaResposta())
            : TypedResults.NotFound(); // de outro cliente = 404 (não confirma que existe)
}
