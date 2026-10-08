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
    public static IEndpointRouteBuilder MapPedidosVersionados(this IEndpointRouteBuilder app)
    {
        // Um "version set" agrupa as versões da MESMA API lógica (é ele que alimenta api-supported-versions).
        var pedidos = app.NewVersionedApi("Pedidos");

        var v1 = pedidos.MapGroup("/v{version:apiVersion}/pedidos")
            .HasDeprecatedApiVersion(Versionamento.V1)
            .WithTags("Pedidos");

        v1.MapGet("/{id:guid}", ObterV1)
            .WithName("ObterPedidoV1")
            .WithSummary("Obtém um pedido (v1, depreciada)")
            .WithDescription("Contrato legado: status numérico e total sem moeda. Migre para a v2 até a data de sunset.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        var v2 = pedidos.MapGroup("/v{version:apiVersion}/pedidos")
            .HasApiVersion(Versionamento.V2)
            .WithTags("Pedidos");

        v2.MapGet("/{id:guid}", ObterV2)
            .WithName("ObterPedidoV2")
            .WithSummary("Obtém um pedido")
            .WithDescription("Status textual e valores monetários com moeda explícita.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        v2.MapPost("/", CriarV2)
            .WithName("CriarPedidoV2")
            .WithSummary("Cria um pedido")
            .WithDescription("O total é calculado no servidor a partir do catálogo.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

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
