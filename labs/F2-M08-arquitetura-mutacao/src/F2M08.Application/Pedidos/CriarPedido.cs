using F2M08.Application.Abstracoes;
using F2M08.Domain.Entidades;
using F2M08.Domain.Repositorios;

namespace F2M08.Application.Pedidos;

public sealed record ItemDoPedido(Guid ProdutoId, int Quantidade);

public sealed record CriarPedido(Guid ClienteId, IReadOnlyList<ItemDoPedido> Itens);

/// <summary>Caso de uso "criar pedido". Depende só de abstrações do domínio (DIP).</summary>
public sealed class CriarPedidoHandler(IPedidoRepository pedidos, IProdutoRepository produtos)
    : ICommandHandler<CriarPedido, Guid>
{
    public async Task<Guid> HandleAsync(CriarPedido command, CancellationToken ct)
    {
        var encontrados = (await produtos.ObterVariosAsync(command.Itens.Select(i => i.ProdutoId), ct))
            .ToDictionary(p => p.Id);

        var pedido = new Pedido(Guid.NewGuid(), command.ClienteId);
        foreach (var item in command.Itens)
        {
            if (!encontrados.TryGetValue(item.ProdutoId, out var produto))
                throw new InvalidOperationException($"Produto {item.ProdutoId} não encontrado.");
            pedido.AdicionarItem(produto, item.Quantidade);
        }

        await pedidos.AdicionarAsync(pedido, ct);
        await pedidos.SalvarAsync(ct);
        return pedido.Id;
    }
}
