using Asp.Versioning;
using F5M02.Api.Configuracao;
using F5M02.Api.Contratos;
using F5M02.Api.Dominio;
using F5M02.Api.Erros;
using Microsoft.AspNetCore.Http.HttpResults;

namespace F5M02.Api.Http;

/// <summary>Endpoints versionados de Pedidos: v1 (depreciada) e v2 convivendo.</summary>
public static class PedidosEndpoints
{
    /// <summary>
    /// TODO (passo 3): mapeie as duas versões no MESMO version set.
    /// <code>
    /// GET  /v1/pedidos/{id}   → ObterV1 (v1 DEPRECIADA)   200 PedidoV1Response | 404
    /// GET  /v2/pedidos/{id}   → ObterV2                   200 PedidoV2Response | 404
    /// POST /v2/pedidos        → CriarV2                   201 + Location | 400 ValidationProblem | 422
    /// </code>
    /// Dicas: <c>app.NewVersionedApi("Pedidos")</c>; grupos com <c>MapGroup("/v{version:apiVersion}/pedidos")</c>;
    /// <c>HasDeprecatedApiVersion(Versionamento.V1)</c> / <c>HasApiVersion(Versionamento.V2)</c>.
    /// Para o OpenAPI (passo 4): <c>WithName</c> (vira operationId: ObterPedidoV1, ObterPedidoV2, CriarPedidoV2),
    /// <c>WithSummary</c>/<c>WithDescription</c>, <c>ProducesProblem(404)</c>, <c>ProducesValidationProblem()</c>, <c>ProducesProblem(422)</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapPedidosVersionados(this IEndpointRouteBuilder app)
    {
        // TODO: var pedidos = app.NewVersionedApi("Pedidos"); ...
        return app;
    }

    // ---------------------------------------------------------------- handlers (PRONTOS)

    private static Results<Ok<PedidoV1Response>, ProblemHttpResult> ObterV1(Guid id, PedidoRepositorio repo) =>
        repo.Obter(id) is { } pedido
            ? TypedResults.Ok(PedidoV1Response.De(pedido))
            : Problemas.PedidoNaoEncontrado(id);

    private static Results<Ok<PedidoV2Response>, ProblemHttpResult> ObterV2(Guid id, PedidoRepositorio repo) =>
        repo.Obter(id) is { } pedido
            ? TypedResults.Ok(PedidoV2Response.De(pedido))
            : Problemas.PedidoNaoEncontrado(id);

    private static Results<Created<PedidoV2Response>, ValidationProblem, ProblemHttpResult> CriarV2(
        CriarPedidoV2Request request, PedidoRepositorio repo, TimeProvider relogio)
    {
        var erros = new Dictionary<string, string[]>();
        if (request.ClienteId == Guid.Empty) erros["clienteId"] = ["Informe o cliente."];
        if (request.Itens is null || request.Itens.Count == 0) erros["itens"] = ["O pedido precisa de pelo menos um item."];
        for (var i = 0; i < (request.Itens?.Count ?? 0); i++)
        {
            if (request.Itens![i].Quantidade <= 0) erros[$"itens[{i}].quantidade"] = ["A quantidade deve ser maior que zero."];
        }
        if (erros.Count > 0) return Problemas.Validacao(erros);

        var itens = new List<ItemPedido>();
        foreach (var item in request.Itens!)
        {
            if (!ProdutosConhecidos.Todos.TryGetValue(item.ProdutoId, out var produto) || !produto.Ativo)
                return Problemas.ProdutoIndisponivel(item.ProdutoId);
            itens.Add(new ItemPedido(produto.Id, produto.Nome, produto.Preco, item.Quantidade));
        }

        var pedido = new Pedido { Id = Guid.NewGuid(), ClienteId = request.ClienteId, CriadoEm = relogio.GetUtcNow(), Itens = itens };
        repo.Adicionar(pedido);

        return TypedResults.Created($"/v2/pedidos/{pedido.Id}", PedidoV2Response.De(pedido));
    }
}
