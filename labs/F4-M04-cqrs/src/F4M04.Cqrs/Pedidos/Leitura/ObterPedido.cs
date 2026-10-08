using F4M04.Cqrs.Abstractions;

namespace F4M04.Cqrs.Pedidos.Leitura;

/// <summary>Query: resumo de um pedido, ou null se não existir no read model.</summary>
public sealed record ObterPedido(Guid PedidoId) : IQuery<PedidoResumo?>;

/// <summary>
/// Lê SÓ do <see cref="BancoDeLeitura"/>: nada de repositório, agregado ou unidade de trabalho.
/// TODO: receba no construtor (primary constructor) apenas o que o lado de leitura precisa.
/// </summary>
public sealed class ObterPedidoHandler : IQueryHandler<ObterPedido, PedidoResumo?>
{
    public Task<PedidoResumo?> HandleAsync(ObterPedido query, CancellationToken ct) =>
        throw new NotImplementedException(
            "TODO: injete o BancoDeLeitura e devolva o PedidoResumo pelo id (ou null). Não injete nada do lado de escrita.");
}
