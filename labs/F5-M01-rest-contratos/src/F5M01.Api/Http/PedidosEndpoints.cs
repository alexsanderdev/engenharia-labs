using System.Text.Json;
using System.Text.RegularExpressions;
using F5M01.Api.Contratos;

namespace F5M01.Api.Http;

/// <summary>
/// A API REST de Pedidos. Recursos e sub-recursos, métodos com a semântica certa, status codes certos,
/// contrato separado do domínio, ETag/If-None-Match/If-Match, paginação e JSON Merge Patch.
/// </summary>
public static partial class PedidosEndpoints
{
    /// <summary>
    /// TODO (passos 1 a 5): mapeie a API REST. O contrato que os testes esperam:
    /// <code>
    /// GET    /pedidos                              200 página (filtros/ordenação/paginação) | 400 ValidationProblem
    /// POST   /pedidos                              201 + Location + ETag | 400 ValidationProblem | 422 produto inativo/inexistente
    /// GET    /pedidos/{id}                         200 + ETag + Cache-Control "private, no-cache" | 304 (If-None-Match) | 404
    /// PATCH  /pedidos/{id}                         200 + ETag | 415 (não é merge-patch) | 412 (If-Match) | 422 | 404
    /// GET    /pedidos/{id}/itens                   200 lista de itens | 404
    /// PUT    /pedidos/{id}/itens/{produtoId}       201 + Location (criou) | 200 (substituiu/repetiu) | 400 | 409 | 412 | 422 | 404
    /// DELETE /pedidos/{id}/itens/{produtoId}       204 (sempre, inclusive repetido) | 409 | 412 | 404
    /// POST   /pedidos/{id}/confirmacao             200 pedido | 409 | 412 | 404
    /// POST   /pedidos/{id}/cancelamento            200 pedido | 409 | 412 | 404
    /// </code>
    /// Use <see cref="Problemas"/> para TODAS as respostas de erro, <see cref="ETags"/> para os headers condicionais,
    /// <see cref="ConsultaPedidos"/> na listagem e <see cref="MergePatch"/> no PATCH.
    /// Dependências: <c>ICatalogo</c>, <c>IPedidoRepositorio</c> e <c>TimeProvider</c> (data de criação) já estão no DI.
    /// </summary>
    public static IEndpointRouteBuilder MapPedidosEndpoints(this IEndpointRouteBuilder app)
    {
        // TODO: var pedidos = app.MapGroup("/pedidos").WithTags("Pedidos"); e os endpoints da tabela acima.
        return app;
    }

    // ---------------------------------------------------------------- apoio (PRONTO)

    public const int TamanhoMaximoObservacao = 500;
    private const string CampoObservacao = "observacao";
    private const string CampoEndereco = "enderecoEntrega";
    private static readonly JsonSerializerOptions JsonWeb = new(JsonSerializerDefaults.Web);

    /// <summary>A parte do pedido editável por PATCH (o "documento" sobre o qual o merge patch é aplicado).</summary>
    private sealed record DadosEntrega(string? Observacao, EnderecoContrato? EnderecoEntrega);

    private static Dictionary<string, string[]> ValidarDadosEntrega(DadosEntrega dados)
    {
        var erros = new Dictionary<string, string[]>();
        if (dados.Observacao is { Length: > TamanhoMaximoObservacao })
            erros[CampoObservacao] = [$"A observação aceita no máximo {TamanhoMaximoObservacao} caracteres."];
        if (dados.EnderecoEntrega is { } e)
        {
            if (string.IsNullOrWhiteSpace(e.Logradouro)) erros["enderecoEntrega.logradouro"] = ["Informe o logradouro."];
            if (string.IsNullOrWhiteSpace(e.Cidade)) erros["enderecoEntrega.cidade"] = ["Informe a cidade."];
            if (e.Cep is null || !CepRegex().IsMatch(e.Cep)) erros["enderecoEntrega.cep"] = ["CEP inválido (formato 00000-000)."];
        }
        return erros;
    }

    [GeneratedRegex(@"^\d{5}-?\d{3}$")]
    private static partial Regex CepRegex();
}
