using F2M02.Solid.Aplicacao;
using F2M02.Solid.Descontos;
using F2M02.Solid.Dominio;
using F2M02.Solid.Infraestrutura;

namespace F2M02.Solid.Legado;

/// <summary>
/// Depois da refatoração: FACHADA fina que preserva a assinatura antiga e só compõe as peças novas.
/// Toda a regra mora em <see cref="Pedido"/>, nas políticas de desconto e no <see cref="CriarPedidoHandler"/>.
/// </summary>
public class PedidoService(BancoDeDadosSql banco, ServidorSmtp smtp, ProdutorKafka kafka)
{
    private readonly CriarPedidoHandler _handler = new(
        new CatalogoSqlDeProdutos(banco),
        new RepositorioSqlDePedidos(banco),
        new PublicadorKafkaDePedidos(kafka),
        new NotificadorPorEmail(smtp),
        CatalogoDePoliticasDeDesconto.Padrao());

    public Pedido CriarPedido(Guid clienteId, string email, List<(Guid ProdutoId, int Quantidade)> itens, string? cupom)
    {
        var comando = new CriarPedidoCommand(
            clienteId,
            email,
            [.. (itens ?? []).Select(i => new ItemSolicitado(i.ProdutoId, i.Quantidade))],
            cupom);

        // A assinatura antiga é síncrona; a fachada bloqueia de propósito (só existe para compatibilidade).
        return _handler.HandleAsync(comando, CancellationToken.None).GetAwaiter().GetResult();
    }
}
