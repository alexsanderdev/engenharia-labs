using F4M01.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace F4M01.Application.Pedidos;

/// <summary>Linha da listagem: DTO achatado, pronto para a borda serializar.</summary>
public sealed record PedidoResumoDto(Guid Id, string Status, decimal Total, DateTimeOffset CriadoEm, int QuantidadeDeItens);

/// <summary>Consulta "pedidos do cliente", do mais recente para o mais antigo.</summary>
/// <remarks>
/// TODO (Passo 3): este caso de uso FUNCIONA, mas só pode ser testado com banco: ele depende do
/// <see cref="OrderFlowDbContext"/> (Infrastructure) e de EF Core. Troque a dependência pela porta
/// <c>IPedidoRepository</c> (método <c>ListarPorClienteAsync</c>) e faça a ordenação/projeção em memória aqui.
/// </remarks>
public sealed class ListarPedidosDoClienteHandler(OrderFlowDbContext db)
{
    public async Task<IReadOnlyList<PedidoResumoDto>> HandleAsync(Guid clienteId, CancellationToken ct = default) =>
        await db.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new PedidoResumoDto(p.Id, p.Status.ToString(), p.Total, p.CriadoEm, p.Itens.Count))
            .ToListAsync(ct);
}
