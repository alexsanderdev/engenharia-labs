using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Contratos;
using F6M02.ServiceBus.Publicacao;
using F6M02.ServiceBus.Topologia;

namespace F6M02.ServiceBus.Pagamentos;

/// <summary>
/// Envia <see cref="SolicitacaoDeCobranca"/> para a fila <see cref="Entidades.FilaPagamentos"/>, que tem
/// <c>RequiresDuplicateDetection = true</c>. O broker descarta, dentro da janela configurada, mensagens com
/// um <c>MessageId</c> que ele já viu. Por isso o MessageId PRECISA ser derivado do negócio.
/// </summary>
public sealed class PublicadorDePagamentos(ServiceBusClient cliente) : IAsyncDisposable
{
    private readonly ServiceBusSender _sender = cliente.CreateSender(Entidades.FilaPagamentos);

    /// <summary>
    /// Mensagem com corpo JSON, <c>ContentType = application/json</c>, <c>Subject = "SolicitacaoDeCobranca"</c>
    /// e <c>MessageId = "cobranca-{PedidoId:N}"</c> (o mesmo pedido gera sempre o mesmo id).
    /// </summary>
    public static ServiceBusMessage CriarMensagem(SolicitacaoDeCobranca solicitacao)
    {
        ArgumentNullException.ThrowIfNull(solicitacao);
        return new ServiceBusMessage(BinaryData.FromObjectAsJson(solicitacao, MensagensDePedido.Json))
        {
            MessageId = $"cobranca-{solicitacao.PedidoId:N}",
            Subject = "SolicitacaoDeCobranca",
            ContentType = MensagensDePedido.ContentTypeJson,
        };
    }

    public Task SolicitarCobrancaAsync(SolicitacaoDeCobranca solicitacao, CancellationToken ct = default) =>
        _sender.SendMessageAsync(CriarMensagem(solicitacao), ct);

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
