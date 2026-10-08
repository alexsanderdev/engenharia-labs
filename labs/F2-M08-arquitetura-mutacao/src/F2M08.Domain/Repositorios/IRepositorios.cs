using F2M08.Domain.Entidades;

namespace F2M08.Domain.Repositorios;

/// <summary>Porta de saída: o domínio diz O QUE precisa; a Infrastructure decide COMO.</summary>
public interface IPedidoRepository
{
    Task<Pedido?> ObterAsync(Guid id, CancellationToken ct);
    Task AdicionarAsync(Pedido pedido, CancellationToken ct);
    Task SalvarAsync(CancellationToken ct);
}

public interface IProdutoRepository
{
    Task<IReadOnlyList<Produto>> ObterVariosAsync(IEnumerable<Guid> ids, CancellationToken ct);
}
