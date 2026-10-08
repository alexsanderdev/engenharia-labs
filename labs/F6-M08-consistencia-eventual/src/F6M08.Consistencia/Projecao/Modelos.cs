using F6M08.Consistencia.Dominio;

namespace F6M08.Consistencia.Projecao;

/// <summary>Uma linha do read model: o pedido como a projeção o enxerga, com a versão do agregado já aplicada.</summary>
public sealed record PedidoResumido(Guid PedidoId, Guid ClienteId, StatusPedido Status, decimal Total, long Versao);

/// <summary>
/// Read model "resumo de pedidos do cliente" (a tela "Meus pedidos" do app).
/// Desnormalizado de propósito: a leitura não faz join nem consulta o lado de escrita.
/// </summary>
public sealed record ResumoDoCliente(Guid ClienteId, IReadOnlyList<PedidoResumido> Pedidos)
{
    public int Quantidade => Pedidos.Count;

    public decimal ValorEmAberto => Pedidos.Where(p => p.Status == StatusPedido.Criado).Sum(p => p.Total);

    public decimal ValorConfirmado => Pedidos.Where(p => p.Status == StatusPedido.Confirmado).Sum(p => p.Total);
}

/// <summary>O que a projeção fez com um evento recebido.</summary>
public enum ResultadoDaAplicacao
{
    /// <summary>Era a próxima versão esperada: aplicado (e talvez tenha destravado eventos adiados).</summary>
    Aplicado,

    /// <summary>Versão já aplicada (duplicata ou evento antigo): ignorado, sem efeito.</summary>
    Ignorado,

    /// <summary>Chegou antes da hora (há lacuna de versão): guardado no buffer até a lacuna ser preenchida.</summary>
    Adiado,
}
