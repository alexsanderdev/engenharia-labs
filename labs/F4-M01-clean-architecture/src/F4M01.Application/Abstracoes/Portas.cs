using F4M01.Domain.Pedidos;
using F4M01.Domain.Produtos;

namespace F4M01.Application.Abstracoes;

// PORTAS (ports) da arquitetura hexagonal: contratos definidos pela Application, na linguagem dela.
// Os ADAPTADORES (adapters) que falam com SQL Server, relógio do sistema etc. ficam na Infrastructure.
// Repare: nenhuma porta menciona DbContext, IQueryable, SqlConnection ou HttpContext.

/// <summary>Persistência do agregado Pedido.</summary>
public interface IPedidoRepository
{
    /// <summary>Adiciona e persiste o pedido (o adaptador decide como: SaveChanges, INSERT, arquivo...).</summary>
    Task AdicionarAsync(Pedido pedido, CancellationToken ct);

    /// <summary>Pedidos do cliente, em qualquer ordem (ordenar é decisão do caso de uso).</summary>
    Task<IReadOnlyList<Pedido>> ListarPorClienteAsync(Guid clienteId, CancellationToken ct);
}

/// <summary>Leitura do catálogo de produtos.</summary>
public interface IProdutoRepository
{
    /// <summary>Busca vários produtos numa única ida ao armazenamento. Ids inexistentes simplesmente não voltam.</summary>
    Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
}

/// <summary>
/// "Que horas são?" é uma dependência externa como qualquer outra. Com a porta, o teste controla o tempo.
/// (Alternativa igualmente válida: injetar <see cref="TimeProvider"/> direto — ver Aula.)
/// </summary>
public interface IRelogio
{
    DateTimeOffset Agora { get; }
}
