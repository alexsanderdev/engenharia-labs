using F4M08.Api.Resultados;

namespace F4M08.Api.Pedidos;

public sealed record ItemPedidoRequest(Guid ProdutoId, int Quantidade);

public sealed record CriarPedidoRequest(Guid ClienteId, IReadOnlyList<ItemPedidoRequest>? Itens);

public sealed record ItemPedidoResponse(Guid ProdutoId, string NomeProduto, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

public sealed record PedidoResponse(Guid Id, Guid ClienteId, string Status, decimal Total, DateTimeOffset CriadoEm, IReadOnlyList<ItemPedidoResponse> Itens)
{
    public static PedidoResponse De(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        return new(pedido.Id, pedido.ClienteId, pedido.Status.ToString(), pedido.Total, pedido.CriadoEm,
            pedido.Itens.Select(i => new ItemPedidoResponse(i.ProdutoId, i.NomeProduto, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList());
    }
}

/// <summary>
/// Casos de uso do módulo Pedidos. Contrato: NENHUM método lança exceção por motivo de negócio;
/// todo "não deu" previsível volta como <see cref="Result"/> com um <see cref="Error"/> de <see cref="PedidoErrors"/>.
/// </summary>
public sealed class PedidosCasosDeUso(ICatalogo catalogo, IPedidoRepositorio pedidos, TimeProvider relogio)
{
    /// <summary>
    /// Valida a entrada (<see cref="PedidoErrors.Validacao"/> com erros por campo: "clienteId", "itens",
    /// "itens[i].quantidade"), busca os produtos (inexistente → <see cref="PedidoErrors.ProdutoInexistente"/>),
    /// cria o agregado (inativo → erro do domínio), persiste e devolve o <see cref="PedidoResponse"/>.
    /// </summary>
    public Task<Result<PedidoResponse>> CriarAsync(CriarPedidoRequest request, CancellationToken ct)
    {
        _ = (catalogo, relogio);
        throw new NotImplementedException(
            "TODO: Validar(request) → se falhar, return validacao.Error; busque cada produto (null → PedidoErrors.ProdutoInexistente); " +
            "Pedido.Criar(...) (falha → return o erro); pedidos.AdicionarAsync; return PedidoResponse.De(pedido). Sem throw de negócio!");
    }

    /// <summary>Pedido pelo id; inexistente → <see cref="PedidoErrors.NaoEncontrado"/>.</summary>
    public Task<Result<PedidoResponse>> ObterAsync(Guid pedidoId, CancellationToken ct) =>
        throw new NotImplementedException("TODO: (await CarregarAsync(id, ct)).Map(PedidoResponse.De).");

    /// <summary>Created → Confirmed. Inexistente → NotFound; outro status → Conflict.</summary>
    public Task<Result<PedidoResponse>> ConfirmarAsync(Guid pedidoId, CancellationToken ct) =>
        throw new NotImplementedException("TODO: carregue o pedido e chame pedido.Confirmar(); propague o erro ou devolva PedidoResponse.De(pedido).");

    /// <summary>Confirmed → Completed. Inexistente → NotFound; outro status → Conflict.</summary>
    public Task<Result<PedidoResponse>> ConcluirAsync(Guid pedidoId, CancellationToken ct) =>
        throw new NotImplementedException("TODO: igual ao ConfirmarAsync, com pedido.Concluir().");

    /// <summary>
    /// Cancela o pedido em nome de <paramref name="clienteSolicitante"/>.
    /// Inexistente → NotFound; pedido de outro cliente → <see cref="PedidoErrors.AcessoNegado"/> (Forbidden);
    /// Completed/Cancelled → Conflict.
    /// </summary>
    public Task<Result> CancelarAsync(Guid pedidoId, Guid clienteSolicitante, CancellationToken ct) =>
        throw new NotImplementedException("TODO: carregue; ClienteId diferente → PedidoErrors.AcessoNegado; senão return pedido.Cancelar().");

    /// <summary>Helper PRONTO: pedido inexistente vira NotFound (repare nas conversões implícitas).</summary>
    private async Task<Result<Pedido>> CarregarAsync(Guid pedidoId, CancellationToken ct)
    {
        var pedido = await pedidos.ObterAsync(pedidoId, ct);
        return pedido is null ? PedidoErrors.NaoEncontrado(pedidoId) : pedido;
    }
}
