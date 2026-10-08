using F4M01.Application.Abstracoes;

namespace F4M01.Application.Pedidos;

/// <summary>Linha da listagem: DTO achatado, pronto para a borda serializar.</summary>
public sealed record PedidoResumoDto(Guid Id, string Status, decimal Total, DateTimeOffset CriadoEm, int QuantidadeDeItens);

/// <summary>Consulta "pedidos do cliente", do mais recente para o mais antigo.</summary>
public sealed class ListarPedidosDoClienteHandler(IPedidoRepository pedidos)
{
    public async Task<IReadOnlyList<PedidoResumoDto>> HandleAsync(Guid clienteId, CancellationToken ct = default)
    {
        var lista = await pedidos.ListarPorClienteAsync(clienteId, ct);

        return lista
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new PedidoResumoDto(p.Id, p.Status.ToString(), p.Total, p.CriadoEm, p.Itens.Count))
            .ToList();
    }
}
