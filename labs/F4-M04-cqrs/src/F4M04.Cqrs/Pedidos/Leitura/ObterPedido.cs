using F4M04.Cqrs.Abstractions;

namespace F4M04.Cqrs.Pedidos.Leitura;

/// <summary>Query: resumo de um pedido, ou null se não existir no read model.</summary>
public sealed record ObterPedido(Guid PedidoId) : IQuery<PedidoResumo?>;

/// <summary>Lê SÓ do <see cref="BancoDeLeitura"/>: nada de repositório, agregado ou unidade de trabalho.</summary>
public sealed class ObterPedidoHandler(BancoDeLeitura leitura) : IQueryHandler<ObterPedido, PedidoResumo?>
{
    public Task<PedidoResumo?> HandleAsync(ObterPedido query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(leitura.Pedidos.TryGetValue(query.PedidoId, out var resumo) ? resumo : null);
    }
}
