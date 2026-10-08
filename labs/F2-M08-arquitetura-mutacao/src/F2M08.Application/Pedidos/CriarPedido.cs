using F2M08.Application.Abstracoes;
using F2M08.Domain.Entidades;
using F2M08.Infrastructure.Persistencia;

namespace F2M08.Application.Pedidos;

public sealed record ItemDoPedido(Guid ProdutoId, int Quantidade);

public sealed record CriarPedido(Guid ClienteId, IReadOnlyList<ItemDoPedido> Itens);

/// <summary>Caso de uso "criar pedido".</summary>
/// <remarks>
/// TODO (Passo 2): o handler recebe as classes CONCRETAS da Infrastructure. Troque por
/// <c>IPedidoRepository</c> e <c>IProdutoRepository</c> (já existem em F2M08.Domain.Repositorios)
/// e depois remova o ProjectReference para a Infrastructure do F2M08.Application.csproj.
/// </remarks>
public sealed class CriarPedidoHandler(PedidoRepositorioEmMemoria pedidos, ProdutoRepositorioEmMemoria produtos)
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
