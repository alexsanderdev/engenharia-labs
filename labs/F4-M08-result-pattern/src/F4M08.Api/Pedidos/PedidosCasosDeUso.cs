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
    public async Task<Result<PedidoResponse>> CriarAsync(CriarPedidoRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validacao = Validar(request);
        if (validacao.IsFailure) return validacao.Error;

        var itens = new List<(Produto, int)>();
        foreach (var item in request.Itens!)
        {
            var produto = await catalogo.ObterAsync(item.ProdutoId, ct);
            if (produto is null) return PedidoErrors.ProdutoInexistente(item.ProdutoId);
            itens.Add((produto, item.Quantidade));
        }

        var criado = Pedido.Criar(request.ClienteId, itens, relogio.GetUtcNow());
        if (criado.IsFailure) return criado.Error;

        await pedidos.AdicionarAsync(criado.Value, ct);
        return PedidoResponse.De(criado.Value);
    }

    /// <summary>Pedido pelo id; inexistente → <see cref="PedidoErrors.NaoEncontrado"/>.</summary>
    public async Task<Result<PedidoResponse>> ObterAsync(Guid pedidoId, CancellationToken ct) =>
        (await CarregarAsync(pedidoId, ct)).Map(PedidoResponse.De);

    /// <summary>Created → Confirmed. Inexistente → NotFound; outro status → Conflict.</summary>
    public async Task<Result<PedidoResponse>> ConfirmarAsync(Guid pedidoId, CancellationToken ct) =>
        (await CarregarAsync(pedidoId, ct)).Bind(p => p.Confirmar().Match(
            onSuccess: () => Result.Success(PedidoResponse.De(p)),
            onFailure: Result.Failure<PedidoResponse>));

    /// <summary>Confirmed → Completed. Inexistente → NotFound; outro status → Conflict.</summary>
    public async Task<Result<PedidoResponse>> ConcluirAsync(Guid pedidoId, CancellationToken ct) =>
        (await CarregarAsync(pedidoId, ct)).Bind(p => p.Concluir().Match(
            onSuccess: () => Result.Success(PedidoResponse.De(p)),
            onFailure: Result.Failure<PedidoResponse>));

    /// <summary>
    /// Cancela o pedido em nome de <paramref name="clienteSolicitante"/>.
    /// Inexistente → NotFound; pedido de outro cliente → <see cref="PedidoErrors.AcessoNegado"/> (Forbidden);
    /// Completed/Cancelled → Conflict.
    /// </summary>
    public async Task<Result> CancelarAsync(Guid pedidoId, Guid clienteSolicitante, CancellationToken ct)
    {
        var carregado = await CarregarAsync(pedidoId, ct);
        if (carregado.IsFailure) return carregado.Error;

        var pedido = carregado.Value;
        if (pedido.ClienteId != clienteSolicitante) return PedidoErrors.AcessoNegado(pedidoId);
        return pedido.Cancelar();
    }

    private async Task<Result<Pedido>> CarregarAsync(Guid pedidoId, CancellationToken ct)
    {
        var pedido = await pedidos.ObterAsync(pedidoId, ct);
        return pedido is null ? PedidoErrors.NaoEncontrado(pedidoId) : pedido;
    }

    private static Result Validar(CriarPedidoRequest request)
    {
        var erros = new Dictionary<string, string[]>();
        if (request.ClienteId == Guid.Empty)
            erros["clienteId"] = ["Informe o cliente."];

        if (request.Itens is null || request.Itens.Count == 0)
        {
            erros["itens"] = ["Um pedido precisa de pelo menos um item."];
        }
        else
        {
            for (var i = 0; i < request.Itens.Count; i++)
            {
                if (request.Itens[i].Quantidade <= 0)
                    erros[$"itens[{i}].quantidade"] = ["A quantidade deve ser maior que zero."];
            }
        }

        return erros.Count == 0 ? Result.Success() : PedidoErrors.Validacao(erros);
    }
}
