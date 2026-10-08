using F6M01.Mensageria.Topologia;

namespace F6M01.Mensageria.Contratos;

/// <summary>Comando (um destinatário) ou evento (N interessados).</summary>
public enum NaturezaDaMensagem
{
    Comando,
    Evento,
}

/// <summary>
/// Metadados de um contrato de mensagem: o nome estável que vai no fio (<see cref="Tipo"/>), a
/// <see cref="Versao"/> do contrato e para onde a mensagem é publicada (exchange + routing key).
/// </summary>
/// <remarks>
/// O <see cref="Tipo"/> NUNCA é o nome da classe .NET: renomear uma classe não pode quebrar
/// consumidores escritos em outra linguagem ou em outro repositório.
/// </remarks>
public sealed record ContratoDeMensagem(
    Type TipoClr,
    string Tipo,
    int Versao,
    NaturezaDaMensagem Natureza,
    string Exchange,
    string RoutingKey);

/// <summary>
/// Catálogo de contratos do OrderFlow: a única fonte de verdade sobre como cada mensagem
/// aparece no broker.
/// </summary>
public static class CatalogoDeContratos
{
    private static readonly Dictionary<Type, ContratoDeMensagem> Contratos = Montar();

    /// <summary>Todos os contratos conhecidos.</summary>
    public static IReadOnlyCollection<ContratoDeMensagem> Todos => Contratos.Values;

    /// <summary>Contrato de <typeparamref name="T"/>.</summary>
    public static ContratoDeMensagem De<T>() where T : IMensagem => De(typeof(T));

    /// <summary>Contrato do tipo informado; lança <see cref="KeyNotFoundException"/> se não houver.</summary>
    public static ContratoDeMensagem De(Type tipo) =>
        Contratos.TryGetValue(tipo, out var contrato)
            ? contrato
            : throw new KeyNotFoundException($"Mensagem sem contrato no catálogo: {tipo.Name}");

    /// <summary>
    /// Monta o catálogo com os 3 contratos do lab:
    /// <list type="bullet">
    /// <item><see cref="ReservarEstoque"/>: tipo <c>orderflow.estoque.reservar-estoque</c>, versão 1, COMANDO,
    /// exchange <see cref="TopologiaOrderFlow.ExchangeComandos"/>, routing key = a fila do destinatário
    /// (<see cref="TopologiaOrderFlow.FilaReservarEstoque"/>).</item>
    /// <item><see cref="PedidoCriado"/>: tipo <c>orderflow.pedidos.pedido-criado</c>, versão 1, EVENTO,
    /// exchange <see cref="TopologiaOrderFlow.ExchangeEventos"/>, routing key <c>pedido.criado</c>.</item>
    /// <item><see cref="PedidoCancelado"/>: tipo <c>orderflow.pedidos.pedido-cancelado</c>, versão 1, EVENTO,
    /// exchange <see cref="TopologiaOrderFlow.ExchangeEventos"/>, routing key <c>pedido.cancelado</c>.</item>
    /// </list>
    /// </summary>
    private static Dictionary<Type, ContratoDeMensagem> Montar()
    {
        ContratoDeMensagem[] contratos =
        [
            new(typeof(ReservarEstoque), "orderflow.estoque.reservar-estoque", 1, NaturezaDaMensagem.Comando,
                TopologiaOrderFlow.ExchangeComandos, TopologiaOrderFlow.FilaReservarEstoque),
            new(typeof(PedidoCriado), "orderflow.pedidos.pedido-criado", 1, NaturezaDaMensagem.Evento,
                TopologiaOrderFlow.ExchangeEventos, "pedido.criado"),
            new(typeof(PedidoCancelado), "orderflow.pedidos.pedido-cancelado", 1, NaturezaDaMensagem.Evento,
                TopologiaOrderFlow.ExchangeEventos, "pedido.cancelado"),
        ];

        return contratos.ToDictionary(c => c.TipoClr);
    }
}
