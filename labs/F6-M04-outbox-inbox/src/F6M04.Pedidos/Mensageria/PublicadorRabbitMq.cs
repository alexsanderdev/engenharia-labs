using System.Text;
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

    public async Task PublicarAsync(MensagemDeSaida mensagem, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mensagem);

        await _trava.WaitAsync(ct);
        try
        {
            var canal = await ObterCanalAsync(ct);

            var propriedades = new BasicProperties
            {
                MessageId = mensagem.MessageId.ToString(),
                Type = mensagem.Tipo,
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                DeliveryMode = DeliveryModes.Persistent, // sobrevive a restart do broker (fila durável)
                Timestamp = new AmqpTimestamp(mensagem.OcorridoEm.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?> { [HeaderChaveDeOrdenacao] = mensagem.ChaveDeOrdenacao },
            };

            // mandatory: true -> se nenhuma fila estiver ligada à routing key, o broker DEVOLVE a mensagem
            // (basic.return) e, com o rastreamento de confirms ligado, o cliente lança PublishException
            // em vez de "publicar no vazio".
            await canal.BasicPublishAsync(
                exchange: _opcoes.Exchange,
                routingKey: mensagem.Tipo,
                mandatory: true,
                basicProperties: propriedades,
                body: Encoding.UTF8.GetBytes(mensagem.Payload),
                cancellationToken: ct);
        }
        catch
        {
            // Canal fechado (broker caiu, exchange inexistente...) não serve para a próxima tentativa.
            if (_canal is { IsOpen: false }) await DescartarAsync();
            throw;
        }
        finally
        {
            _trava.Release();
        }
    }

    private async Task<IChannel> ObterCanalAsync(CancellationToken ct)
    {
        if (_canal is { IsOpen: true }) return _canal;

        await DescartarAsync();

        var fabrica = new ConnectionFactory
        {
            Uri = new Uri(_opcoes.ConnectionString),
            RequestedConnectionTimeout = _opcoes.TempoMaximoDeConexao,
            ClientProvidedName = "f6m04-outbox-publicador",
        };

        _conexao = await fabrica.CreateConnectionAsync(ct);
        _canal = await _conexao.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);
        return _canal;
    }

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
