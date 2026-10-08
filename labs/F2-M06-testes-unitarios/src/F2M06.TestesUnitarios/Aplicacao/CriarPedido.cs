using F2M06.TestesUnitarios.Dominio;

namespace F2M06.TestesUnitarios.Aplicacao;

/// <summary>Linha do comando: o cliente informa produto e quantidade — NUNCA o preço.</summary>
public sealed record ItemDoComando(Guid ProdutoId, int Quantidade);

/// <summary>Entrada do caso de uso "criar pedido".</summary>
public sealed record CriarPedidoComando(Guid ClienteId, IReadOnlyList<ItemDoComando> Itens);

/// <summary>Resultado do caso de uso: sucesso com id e total, ou falha com mensagem.</summary>
public sealed record ResultadoCriarPedido(bool Sucesso, Guid PedidoId, decimal Total, string? Erro)
{
    public static ResultadoCriarPedido Ok(Guid pedidoId, decimal total) => new(true, pedidoId, total, null);
    public static ResultadoCriarPedido Falha(string erro) => new(false, Guid.Empty, 0m, erro);
}

/// <summary>
/// Caso de uso "criar pedido" (código de produção PRONTO — o lab é sobre testá-lo bem).
/// Fluxo: valida itens → limite de pedidos em aberto do cliente → busca produtos no catálogo →
/// cria o pedido (regras de domínio) com a hora do <see cref="TimeProvider"/> → salva → publica <see cref="PedidoCriado"/>.
/// Em qualquer falha, nada é salvo e nada é publicado.
/// </summary>
public sealed class CriarPedido(
    IRepositorioDePedidos repositorio,
    ICatalogoDeProdutos catalogo,
    IPublicadorDeEventos publicador,
    TimeProvider relogio)
{
    /// <summary>Quantos pedidos em aberto (Created ou Confirmed) um cliente pode ter ao mesmo tempo.</summary>
    public const int LimiteDePedidosEmAberto = 3;

    public async Task<ResultadoCriarPedido> ExecutarAsync(CriarPedidoComando comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        if (comando.Itens.Count == 0)
            return ResultadoCriarPedido.Falha("Pedido precisa de ao menos um item.");

        var emAberto = await repositorio.ContarEmAbertoDoClienteAsync(comando.ClienteId, cancellationToken);
        if (emAberto >= LimiteDePedidosEmAberto)
            return ResultadoCriarPedido.Falha(
                $"Cliente já tem {emAberto} pedidos em aberto (limite: {LimiteDePedidosEmAberto}).");

        var linhas = new List<(Produto, int)>(comando.Itens.Count);
        foreach (var item in comando.Itens)
        {
            var produto = await catalogo.ObterPorIdAsync(item.ProdutoId, cancellationToken);
            if (produto is null)
                return ResultadoCriarPedido.Falha($"Produto {item.ProdutoId} não encontrado.");
            linhas.Add((produto, item.Quantidade));
        }

        Pedido pedido;
        try
        {
            pedido = Pedido.Criar(comando.ClienteId, linhas, relogio.GetUtcNow());
        }
        catch (RegraDeNegocioException ex)
        {
            return ResultadoCriarPedido.Falha(ex.Message);
        }

        await repositorio.AdicionarAsync(pedido, cancellationToken);
        await publicador.PublicarAsync(
            new PedidoCriado(pedido.Id, pedido.ClienteId, pedido.Total, pedido.CriadoEm), cancellationToken);

        return ResultadoCriarPedido.Ok(pedido.Id, pedido.Total);
    }
}
