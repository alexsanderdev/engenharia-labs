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
    public Task<Pedido> HandleAsync(CriarPedidoCommand command, CancellationToken ct)
    {
        _ = (catalogo, pedidos, eventos, notificador, politicas);
        throw new NotImplementedException("TODO: orquestre politicas.Obter → catalogo (ids distintos, uma chamada) → Pedido.Criar → salvar → publicar → notificar");
    }
}
