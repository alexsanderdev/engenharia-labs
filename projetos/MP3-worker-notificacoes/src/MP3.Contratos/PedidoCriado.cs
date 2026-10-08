namespace MP3.Contratos;

/// <summary>
/// Evento de integração publicado pela API do OrderFlow (via Outbox) quando um pedido é criado.
/// <para>
/// É um CONTRATO PÚBLICO: outros times dependem dele. Só evolua de forma compatível
/// (campo novo opcional); mudança incompatível = novo <see cref="VersaoDoContrato"/> e período de convivência.
/// </para>
/// <para>
/// <see cref="EventoId"/> identifica o FATO (não a mensagem): é a chave de idempotência do consumidor.
/// O publicador usa o mesmo valor como <c>MessageId</c> do Service Bus.
/// </para>
/// <para>
/// Decisão consciente: o evento carrega o contato do cliente (event-carried state transfer) para o worker
/// não precisar chamar a API de Clientes. Custo: dado pessoal trafegando no broker — ver o ADR do MP3.
/// </para>
/// </summary>
public sealed record PedidoCriado(
    Guid EventoId,
    Guid PedidoId,
    Guid ClienteId,
    string NomeDoCliente,
    string? Email,
    string? Telefone,
    string? CanalPreferido,
    decimal ValorTotal,
    DateTimeOffset CriadoEm,
    int VersaoDoContrato = 1);
