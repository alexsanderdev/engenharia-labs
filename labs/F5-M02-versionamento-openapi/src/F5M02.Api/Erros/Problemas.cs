using Microsoft.AspNetCore.Http.HttpResults;

namespace F5M02.Api.Erros;

/// <summary>
/// Erros de negócio da API (PRONTO). Cada um define título, status e um <c>code</c> estável.
/// O resto (<c>traceId</c>, <c>instance</c>, <c>type</c> e o <c>code</c> padrão de erros que não passam por aqui)
/// é responsabilidade do <see cref="ProblemDetailsPadrao"/>.
/// </summary>
public static class Problemas
{
    public const string CodigoValidacao = "pedido.validacao";
    public const string CodigoNaoEncontrado = "pedido.nao_encontrado";
    public const string CodigoProdutoIndisponivel = "produto.indisponivel";

    public static ProblemHttpResult PedidoNaoEncontrado(Guid id) =>
        TypedResults.Problem(
            title: "Pedido não encontrado",
            detail: $"Não existe pedido com id {id}.",
            statusCode: StatusCodes.Status404NotFound,
            extensions: new Dictionary<string, object?> { ["code"] = CodigoNaoEncontrado });

    public static ValidationProblem Validacao(IDictionary<string, string[]> erros) =>
        TypedResults.ValidationProblem(
            erros,
            title: "Requisição inválida",
            detail: "Um ou mais campos são inválidos.",
            extensions: new Dictionary<string, object?> { ["code"] = CodigoValidacao });

    public static ProblemHttpResult ProdutoIndisponivel(Guid produtoId) =>
        TypedResults.Problem(
            title: "Produto indisponível",
            detail: $"O produto {produtoId} não existe ou está inativo.",
            statusCode: StatusCodes.Status422UnprocessableEntity,
            extensions: new Dictionary<string, object?> { ["code"] = CodigoProdutoIndisponivel });
}
