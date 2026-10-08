using RabbitMQ.Client;

namespace F6M01.Mensageria.Topologia;

/// <summary>
/// Topologia de mensageria do OrderFlow no RabbitMQ.
/// <code>
///  COMANDOS (1 destinatário)                       EVENTOS (N interessados)
///  orderflow.comandos (direct)                     orderflow.eventos (topic)
///     └─ "estoque.reservar-estoque"                   ├─ "pedido.criado" ─► notificacao.pedido-criado
///          ─► estoque.reservar-estoque                └─ "pedido.*"      ─► fidelidade.eventos-de-pedido
///             (2+ instâncias do Estoque competem)
/// </code>
/// </summary>
/// <remarks>
/// A fila de COMANDO pertence ao destinatário (Estoque); várias instâncias dele consomem a MESMA fila
/// (competing consumers). As filas de EVENTO pertencem a cada assinante: cada um tem a SUA fila e recebe
/// a sua cópia. O publicador do evento não conhece nenhuma delas: só a exchange.
/// </remarks>
public static class TopologiaOrderFlow
{
    /// <summary>Exchange <c>direct</c> de comandos: routing key = nome da fila do destinatário.</summary>
    public const string ExchangeComandos = "orderflow.comandos";

    /// <summary>Exchange <c>topic</c> de eventos de domínio: routing keys <c>pedido.criado</c>, <c>pedido.cancelado</c>…</summary>
    public const string ExchangeEventos = "orderflow.eventos";

    /// <summary>Fila do Estoque para o comando <c>ReservarEstoque</c>.</summary>
    public const string FilaReservarEstoque = "estoque.reservar-estoque";

    /// <summary>Fila da Notificação: só quer saber de pedidos criados (binding <c>pedido.criado</c>).</summary>
    public const string FilaNotificacaoPedidoCriado = "notificacao.pedido-criado";

    /// <summary>Fila da Fidelidade: quer todo evento de pedido de um nível (binding <c>pedido.*</c>).</summary>
    public const string FilaFidelidadeEventosDePedido = "fidelidade.eventos-de-pedido";

    /// <summary>Todas as exchanges declaradas por <see cref="DeclararAsync"/>.</summary>
    public static IReadOnlyList<string> Exchanges { get; } = [ExchangeComandos, ExchangeEventos];

    /// <summary>Todas as filas declaradas por <see cref="DeclararAsync"/>.</summary>
    public static IReadOnlyList<string> Filas { get; } =
        [FilaReservarEstoque, FilaNotificacaoPedidoCriado, FilaFidelidadeEventosDePedido];

    /// <summary>
    /// Declara (de forma idempotente) a topologia acima:
    /// <list type="number">
    /// <item><see cref="ExchangeComandos"/> do tipo <c>direct</c> e <see cref="ExchangeEventos"/> do tipo
    /// <c>topic</c>, ambas DURÁVEIS e sem auto-delete;</item>
    /// <item>as 3 filas DURÁVEIS, não exclusivas e sem auto-delete;</item>
    /// <item>os bindings: comandos → fila do Estoque com a routing key = nome da fila; eventos → Notificação
    /// com <c>pedido.criado</c>; eventos → Fidelidade com <c>pedido.*</c>.</item>
    /// </list>
    /// </summary>
    public static async Task DeclararAsync(IChannel canal, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(canal);

        await Task.CompletedTask;
        throw new NotImplementedException(
            "TODO (Passo 4): ExchangeDeclareAsync (direct e topic, duráveis), QueueDeclareAsync (3 filas duráveis) " +
            "e QueueBindAsync (comando → fila do Estoque; pedido.criado → Notificação; pedido.* → Fidelidade)");
    }
}
