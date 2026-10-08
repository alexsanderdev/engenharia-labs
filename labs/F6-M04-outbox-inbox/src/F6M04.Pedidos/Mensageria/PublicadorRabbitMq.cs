using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace F6M04.Pedidos.Mensageria;

/// <summary>
/// Publica no RabbitMQ COM publisher confirms: só retorna depois que o broker confirmou (ack) que aceitou
/// e roteou a mensagem. Sem confirmação, "publiquei" significa só "escrevi no socket" — e a Outbox
/// marcaria como processada uma mensagem que pode ter se perdido.
/// </summary>
/// <remarks>
/// Uma conexão e um canal por publicador (singleton), reaproveitados entre chamadas. Se o broker cair, o canal
/// fecha; a próxima publicação descarta o canal morto e tenta reabrir (a Outbox cuida do retry).
/// </remarks>
public sealed class PublicadorRabbitMq(IOptions<RabbitMqOptions> opcoes) : IPublicadorDeMensagens, IAsyncDisposable
{
    /// <summary>Header com a chave de ordenação (o pedido), para consumidores que particionam por agregado.</summary>
    public const string HeaderChaveDeOrdenacao = "x-chave-de-ordenacao";

    private readonly RabbitMqOptions _opcoes = opcoes.Value;
    private readonly SemaphoreSlim _trava = new(1, 1);
    private IConnection? _conexao;
    private IChannel? _canal;

    /// <summary>
    /// Publica UMA mensagem e só retorna depois do ack do broker. Erro de conexão, nack ou mensagem devolvida
    /// (sem fila para a routing key) viram exceção — e a Outbox tenta de novo depois.
    /// </summary>
    public Task PublicarAsync(MensagemDeSaida mensagem, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 2): sob _trava, obtenha o canal (ObterCanalAsync), monte BasicProperties (MessageId, Type, " +
            "ContentType application/json, DeliveryMode Persistent, Timestamp, header HeaderChaveDeOrdenacao) e chame " +
            "canal.BasicPublishAsync(_opcoes.Exchange, mensagem.Tipo, mandatory: true, propriedades, corpo UTF-8, ct). " +
            "Em exceção, descarte o canal se ele fechou (DescartarAsync) e relance.");

    /// <summary>Reaproveita o canal aberto ou abre conexão + canal COM publisher confirms.</summary>
    private Task<IChannel> ObterCanalAsync(CancellationToken ct) =>
        throw new NotImplementedException(
            "TODO (Passo 2): se _canal estiver aberto, devolva-o. Senão: DescartarAsync(); ConnectionFactory { Uri, " +
            "RequestedConnectionTimeout = _opcoes.TempoMaximoDeConexao }; _conexao = CreateConnectionAsync; _canal = " +
            "CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct).");

    private async Task DescartarAsync()
    {
        try
        {
            if (_canal is not null) await _canal.DisposeAsync();
            if (_conexao is not null) await _conexao.DisposeAsync();
        }
        catch (Exception)
        {
            // Já estava morto: nada a fazer.
        }
        finally
        {
            _canal = null;
            _conexao = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DescartarAsync();
        _trava.Dispose();
    }
}
