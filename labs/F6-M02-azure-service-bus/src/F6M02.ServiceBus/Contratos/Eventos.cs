namespace F6M02.ServiceBus.Contratos;

/// <summary>
/// Evento de integração publicado pelo módulo de Pedidos do OrderFlow quando um pedido é criado.
/// Vai para o tópico <c>pedidos</c>; cada consumidor (notificação, antifraude, fidelidade) tem a sua subscription.
/// </summary>
/// <param name="PedidoId">Identificador do pedido (também usado para derivar o MessageId).</param>
/// <param name="ClienteId">Cliente dono do pedido.</param>
/// <param name="ValorTotal">Total calculado no servidor, em reais.</param>
/// <param name="Segmento">Segmento do cliente: <c>comum</c> ou <c>vip</c>.</param>
/// <param name="Canal">Canal de origem: <c>site</c>, <c>app</c> ou <c>loja</c>.</param>
/// <param name="CriadoEm">Momento da criação do pedido.</param>
public sealed record PedidoCriado(
    Guid PedidoId,
    Guid ClienteId,
    decimal ValorTotal,
    string Segmento,
    string Canal,
    DateTimeOffset CriadoEm);

/// <summary>
/// Evento do ciclo de vida de UM pedido (criado, pago, separado, enviado...).
/// Eventos do mesmo pedido precisam ser processados na ordem em que aconteceram: por isso vão para a
/// fila com sessions <c>pedidos-eventos</c>, com <c>SessionId = PedidoId</c>.
/// </summary>
/// <param name="PedidoId">Pedido ao qual o evento pertence (vira o SessionId).</param>
/// <param name="Sequencia">Ordem do evento dentro do pedido: 1, 2, 3...</param>
/// <param name="Tipo">Nome do evento (ex.: <c>PedidoPago</c>).</param>
public sealed record EventoDoPedido(Guid PedidoId, int Sequencia, string Tipo);

/// <summary>
/// Comando enviado para a fila <c>pagamentos</c> pedindo a cobrança de um pedido.
/// A fila tem detecção de duplicatas: o mesmo pedido enviado duas vezes (retry do publicador,
/// clique duplo) deve gerar UMA mensagem só.
/// </summary>
public sealed record SolicitacaoDeCobranca(Guid PedidoId, decimal Valor);

/// <summary>Lembrete agendado para cobrar o cliente que ainda não pagou o pedido.</summary>
public sealed record LembreteDePagamento(Guid PedidoId, Guid ClienteId);
