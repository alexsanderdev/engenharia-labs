using F5M01.Api.Dominio;

namespace F5M01.Api.Contratos;

// ---------------------------------------------------------------------------------------------
// CONTRATO PÚBLICO da API de Pedidos. Estes tipos são o que o cliente vê — mudar um nome aqui
// é breaking change. Eles NÃO são o domínio: o domínio pode ser refatorado à vontade desde que
// o mapeamento (os métodos De) continue produzindo o mesmo JSON.
// ---------------------------------------------------------------------------------------------

/// <summary>Corpo do <c>POST /pedidos</c>.</summary>
public sealed record CriarPedidoRequest(Guid ClienteId, List<ItemPedidoRequest>? Itens, string? Observacao = null);

public sealed record ItemPedidoRequest(Guid ProdutoId, int Quantidade);

/// <summary>Corpo do <c>PUT /pedidos/{id}/itens/{produtoId}</c>: a representação COMPLETA do item (o produto vem da URL).</summary>
public sealed record DefinirItemRequest(int Quantidade);

public sealed record EnderecoContrato(string Logradouro, string Cidade, string Cep);

/// <summary>Representação completa de um pedido (<c>GET /pedidos/{id}</c>).</summary>
public sealed record PedidoResponse(
    Guid Id,
    Guid ClienteId,
    string Status,
    DateTimeOffset CriadoEm,
    string? Observacao,
    EnderecoContrato? EnderecoEntrega,
    IReadOnlyList<ItemPedidoResponse> Itens,
    decimal Total)
{
    /// <summary>
    /// Domínio → contrato. Só os campos públicos: nada de <c>CustoInterno</c>, <c>NotaInternaAntifraude</c>
    /// ou <c>Versao</c> (a versão sai no header ETag, não no corpo). O status vai como texto ("Created").
    /// </summary>
    public static PedidoResponse De(Pedido pedido)
    {
        throw new NotImplementedException(
            "TODO (passo 1): mapeie Pedido → PedidoResponse. Status como texto (ToString()), endereço → EnderecoContrato, " +
            "itens → ItemPedidoResponse.De. Sem CustoInterno, NotaInternaAntifraude nem Versao.");
    }
}

public sealed record ItemPedidoResponse(Guid ProdutoId, string Nome, decimal PrecoUnitario, int Quantidade, decimal Subtotal)
{
    /// <summary>Domínio → contrato (sem o custo unitário).</summary>
    public static ItemPedidoResponse De(ItemPedido item)
    {
        throw new NotImplementedException("TODO (passo 1): mapeie ItemPedido → ItemPedidoResponse (Nome = NomeProduto; sem CustoUnitario).");
    }
}

/// <summary>Representação resumida para listas (<c>GET /pedidos</c>): lista não precisa carregar itens.</summary>
public sealed record PedidoResumoResponse(Guid Id, Guid ClienteId, string Status, DateTimeOffset CriadoEm, int QuantidadeItens, decimal Total)
{
    public static PedidoResumoResponse De(Pedido pedido)
    {
        throw new NotImplementedException("TODO (passo 5): mapeie Pedido → PedidoResumoResponse (QuantidadeItens = número de itens).");
    }
}

/// <summary>Envelope de página: dados + metadados + links de navegação (relações IANA: first, prev, next, last).</summary>
public sealed record PaginaResponse<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int TamanhoPagina,
    int TotalItens,
    int TotalPaginas,
    LinksPaginacao Links);

/// <summary>URLs relativas que preservam filtros e ordenação. <c>Prev</c>/<c>Next</c> são null quando não existem.</summary>
public sealed record LinksPaginacao(string Self, string First, string? Prev, string? Next, string Last);
