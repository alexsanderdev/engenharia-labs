using F4M02.Pedidos.Domain.Comum;
using F4M02.Pedidos.Domain.Pedidos;
using F4M02.Pedidos.Domain.ValueObjects;

namespace F4M02.Pedidos.Domain.Descontos;

/// <summary>
/// Domain service: a regra de desconto depende do <b>cliente</b> (histórico, programa VIP), que não
/// pertence ao agregado Pedido. Em vez de enfiar dados do cliente no pedido (agregado inchado) ou
/// deixar a regra num "PedidoService" de aplicação (domínio anêmico), ela vira um serviço do domínio,
/// sem estado, nomeado na linguagem do negócio.
/// </summary>
/// <remarks>
/// Regra do negócio:
/// <list type="bullet">
/// <item>Cliente VIP: <see cref="PercentualVip"/>% do subtotal.</item>
/// <item>Cliente fiel (≥ <see cref="PedidosParaSerFiel"/> pedidos concluídos): <see cref="PercentualFiel"/>%.</item>
/// <item>VIP e fiel não acumulam: vale o maior.</item>
/// <item>Desconto limitado a <see cref="TetoPorPedido"/> na moeda do pedido.</item>
/// <item>Arredondamento: 2 casas, <see cref="MidpointRounding.AwayFromZero"/> (ver <see cref="Dinheiro.Percentual"/>).</item>
/// </list>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
    Justification = "Domain service sem estado, mas instanciável: a aplicação recebe por DI e pode trocar a política.")]
public sealed class PoliticaDeDesconto
{
    public const decimal PercentualVip = 10m;
    public const decimal PercentualFiel = 5m;
    public const int PedidosParaSerFiel = 5;
    public const decimal TetoPorPedido = 50m;

    /// <summary>Calcula o desconto a que o cliente tem direito neste pedido, sem alterar o pedido.</summary>
    /// <exception cref="RegraDeNegocioVioladaException"><see cref="Regras.ClienteDiferente"/> se o perfil não for do dono do pedido.</exception>
    public Dinheiro Calcular(Pedido pedido, PerfilDoCliente cliente)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        ArgumentNullException.ThrowIfNull(cliente);
        if (cliente.ClienteId != pedido.ClienteId)
            throw new RegraDeNegocioVioladaException(Regras.ClienteDiferente, "O perfil informado não é do cliente dono do pedido.");

        var percentual = cliente switch
        {
            { Vip: true } => PercentualVip,
            { PedidosConcluidos: >= PedidosParaSerFiel } => PercentualFiel,
            _ => 0m,
        };

        var teto = new Dinheiro(TetoPorPedido, pedido.Moeda);
        return Dinheiro.Menor(pedido.Subtotal.Percentual(percentual), teto);
    }

    /// <summary>Calcula e aplica o desconto no pedido. Devolve o valor aplicado.</summary>
    public Dinheiro Aplicar(Pedido pedido, PerfilDoCliente cliente)
    {
        var desconto = Calcular(pedido, cliente);
        pedido.AplicarDesconto(desconto);
        return desconto;
    }
}
