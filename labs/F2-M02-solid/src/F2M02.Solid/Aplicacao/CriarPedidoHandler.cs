using F2M02.Solid.Descontos;
using F2M02.Solid.Dominio;

namespace F2M02.Solid.Aplicacao;

public sealed record ItemSolicitado(Guid ProdutoId, int Quantidade);

public sealed record CriarPedidoCommand(Guid ClienteId, string EmailCliente, IReadOnlyList<ItemSolicitado> Itens, string? Cupom = null);

/// <summary>
/// Caso de uso "criar pedido": só ORQUESTRA. A regra está em <see cref="Pedido.Criar"/> e nas
/// políticas de desconto; os detalhes externos chegam por abstrações injetadas (DIP).
/// </summary>
public sealed class CriarPedidoHandler(
    ICatalogoDeProdutos catalogo,
    IRepositorioDePedidos pedidos,
    IOrderEventPublisher eventos,
    INotificadorDeCliente notificador,
    CatalogoDePoliticasDeDesconto politicas)
{
    /// <summary>
    /// Fluxo: resolve a política do cupom (cupom inválido falha ANTES de qualquer I/O) → carrega os produtos
    /// (produto inexistente → <see cref="PedidoInvalidoException"/> "Produto não encontrado: {id}") →
    /// <see cref="Pedido.Criar"/> → salva → publica o evento → notifica o cliente. Se salvar falhar,
    /// nada é publicado nem notificado.
    /// </summary>
    public async Task<Pedido> HandleAsync(CriarPedidoCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var politica = politicas.Obter(command.Cupom);
        var linhas = await MontarLinhasAsync(command.Itens, ct);
        var pedido = Pedido.Criar(command.ClienteId, linhas, politica);

        await pedidos.SalvarAsync(pedido, ct);
        await eventos.PublishOrderCreatedAsync(pedido, ct);
        await notificador.NotificarPedidoCriadoAsync(pedido, command.EmailCliente, ct);

        return pedido;
    }

    private async Task<List<LinhaDoPedido>> MontarLinhasAsync(IReadOnlyList<ItemSolicitado> itens, CancellationToken ct)
    {
        var ids = itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = (await catalogo.ObterPorIdsAsync(ids, ct)).ToDictionary(p => p.Id);

        return [.. itens.Select(item => new LinhaDoPedido(
            produtos.GetValueOrDefault(item.ProdutoId) ?? throw new PedidoInvalidoException($"Produto não encontrado: {item.ProdutoId}"),
            item.Quantidade))];
    }
}
