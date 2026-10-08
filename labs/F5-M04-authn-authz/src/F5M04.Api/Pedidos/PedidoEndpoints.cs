using System.Security.Claims;
using F5M04.Api.Catalogo;
using F5M04.Api.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace F5M04.Api.Pedidos;

public sealed record ItemPedidoRequest(Guid ProdutoId, int Quantidade);

/// <summary>
/// Repare: NÃO existe ClienteId no contrato. O dono do pedido é sempre o <c>sub</c> do token.
/// Um campo "clienteId" enviado no JSON é simplesmente ignorado.
/// </summary>
public sealed record CriarPedidoRequest(IReadOnlyList<ItemPedidoRequest>? Itens);

public sealed record ItemPedidoResponse(Guid ProdutoId, string Nome, decimal PrecoUnitario, int Quantidade);

public sealed record PedidoResponse(Guid Id, Guid ClienteId, string Status, decimal Total, IReadOnlyList<ItemPedidoResponse> Itens)
{
    public static PedidoResponse De(Pedido p) => new(p.Id, p.ClienteId, p.Status.ToString(), p.Total,
        [.. p.Itens.Select(i => new ItemPedidoResponse(i.ProdutoId, i.Nome, i.PrecoUnitario, i.Quantidade))]);
}

public static class PedidoEndpoints
{
    public static IEndpointRouteBuilder MapPedidoEndpoints(this IEndpointRouteBuilder app)
    {
        var pedidos = app.MapGroup("/pedidos");

        // Sem política explícita: protegido pela FallbackPolicy (usuário autenticado).
        pedidos.MapGet("/", ListarMeus);

        // Criar: precisa ser Cliente E ter o escopo pedidos.write (várias políticas = todas precisam passar).
        pedidos.MapPost("/", Criar)
            .RequireAuthorization(Politicas.Cliente, Politicas.PedidosEscrita);

        // Ler: autenticado (fallback) + autorização por recurso dentro do handler.
        pedidos.MapGet("/{id:guid}", Obter).WithName("ObterPedido");

        // Cancelar é escrita: exige o escopo; quem pode cancelar ESTE pedido é decidido no handler.
        pedidos.MapPost("/{id:guid}/cancelar", Cancelar)
            .RequireAuthorization(Politicas.PedidosEscrita);

        return app;
    }

    private static Ok<PedidoResponse[]> ListarMeus(ClaimsPrincipal user, IPedidoRepositorio repositorio) =>
        TypedResults.Ok(user.TryObterClienteId(out var clienteId)
            ? [.. repositorio.ListarDoCliente(clienteId).Select(PedidoResponse.De)]
            : Array.Empty<PedidoResponse>());

    private static Results<CreatedAtRoute<PedidoResponse>, ValidationProblem, ForbidHttpResult> Criar(
        CriarPedidoRequest request, ClaimsPrincipal user, ICatalogo catalogo, IPedidoRepositorio repositorio, TimeProvider relogio)
    {
        // A política Cliente já exige "sub", mas o handler não confia cegamente: sub precisa ser um id de cliente válido.
        if (!user.TryObterClienteId(out var clienteId)) return TypedResults.Forbid();

        var erros = new Dictionary<string, string[]>();
        var itens = new List<ItemPedido>();
        if (request.Itens is not { Count: > 0 })
        {
            erros["itens"] = ["Informe ao menos um item."];
        }
        else
        {
            for (var i = 0; i < request.Itens.Count; i++)
            {
                var item = request.Itens[i];
                var produto = catalogo.Obter(item.ProdutoId);
                if (item.Quantidade <= 0) erros[$"itens[{i}].quantidade"] = ["A quantidade deve ser maior que zero."];
                else if (produto is not { Ativo: true }) erros[$"itens[{i}].produtoId"] = ["Produto inexistente ou inativo."];
                else itens.Add(new ItemPedido(produto.Id, produto.Nome, produto.Preco, item.Quantidade));
            }
        }
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var pedido = Pedido.Criar(clienteId, itens, relogio.GetUtcNow());
        repositorio.Adicionar(pedido);
        return TypedResults.CreatedAtRoute(PedidoResponse.De(pedido), "ObterPedido", new { id = pedido.Id });
    }

    /// <summary>
    /// GET /pedidos/{id}: carrega o pedido e pergunta ao <see cref="IAuthorizationService"/> se o usuário pode LER.
    /// Inexistente e "de outro cliente" devolvem o MESMO 404: a API não confirma que o id existe.
    /// </summary>
    private static async Task<Results<Ok<PedidoResponse>, NotFound>> Obter(
        Guid id, ClaimsPrincipal user, IPedidoRepositorio repositorio, IAuthorizationService autorizacao)
    {
        var pedido = repositorio.Obter(id);
        if (pedido is null) return TypedResults.NotFound();

        var leitura = await autorizacao.AuthorizeAsync(user, pedido, OperacoesPedido.Ler);
        if (!leitura.Succeeded) return TypedResults.NotFound();

        return TypedResults.Ok(PedidoResponse.De(pedido));
    }

    /// <summary>
    /// POST /pedidos/{id}/cancelar: quem não pode nem LER recebe 404 (não sabe que existe);
    /// quem pode ler mas não pode cancelar (Admin) recebe 403; transição inválida é 409.
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, ProblemHttpResult>> Cancelar(
        Guid id, ClaimsPrincipal user, IPedidoRepositorio repositorio, IAuthorizationService autorizacao)
    {
        var pedido = repositorio.Obter(id);
        if (pedido is null) return TypedResults.NotFound();

        if (!(await autorizacao.AuthorizeAsync(user, pedido, OperacoesPedido.Ler)).Succeeded)
            return TypedResults.NotFound();

        if (!(await autorizacao.AuthorizeAsync(user, pedido, OperacoesPedido.Cancelar)).Succeeded)
            return TypedResults.Forbid();

        if (!pedido.Cancelar())
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Transição inválida", detail: $"Pedido no status {pedido.Status} não pode ser cancelado.");

        return TypedResults.NoContent();
    }
}
