using System.Net;
using System.Net.Sockets;
using System.Text;
using F6M04.Notificacoes.Mensageria;
using RabbitMQ.Client;

namespace F6M04.Tests.Infra;

/// <summary>Uma mensagem lida da fila pelo teste.</summary>
public sealed record MensagemLida(string? MessageId, string? Tipo, bool Persistente, string? ContentType, string Corpo, IDictionary<string, object?>? Headers);

/// <summary>
/// PRONTO. "Olhos" do teste dentro do RabbitMQ: declara a topologia do teste, lê mensagens (basic.get),
/// conta filas e publica mensagens "cruas" (para simular duplicatas e lixo).
/// </summary>
public sealed class BrokerDeTeste : IAsyncDisposable
{
    private readonly IConnection _conexao;
    private readonly IChannel _canal;

    private BrokerDeTeste(IConnection conexao, IChannel canal, NotificacoesRabbitMqOptions topologia)
    {
        _conexao = conexao;
        _canal = canal;
        Topologia = topologia;
    }

    public NotificacoesRabbitMqOptions Topologia { get; }

    public static async Task<BrokerDeTeste> ConectarAsync(string amqpUri, NotificacoesRabbitMqOptions topologia)
    {
        var fabrica = new ConnectionFactory { Uri = new Uri(amqpUri), ClientProvidedName = "f6m04-testes" };
        var conexao = await fabrica.CreateConnectionAsync();
        var canal = await conexao.CreateChannelAsync();
        await TopologiaDeNotificacoes.DeclararAsync(canal, topologia);
        return new BrokerDeTeste(conexao, canal, topologia);
    }

    /// <summary>Lê (e remove) exatamente <paramref name="quantidade"/> mensagens da fila, esperando até chegarem.</summary>
    public async Task<IReadOnlyList<MensagemLida>> LerAsync(string fila, int quantidade)
    {
        var lidas = new List<MensagemLida>();
        await Esperas.Eventualmente(async () =>
        {
            while (lidas.Count < quantidade && await _canal.BasicGetAsync(fila, autoAck: true) is { } r)
            {
                lidas.Add(new MensagemLida(
                    r.BasicProperties.MessageId,
                    r.BasicProperties.Type,
                    r.BasicProperties.Persistent,
                    r.BasicProperties.ContentType,
                    Encoding.UTF8.GetString(r.Body.Span),
                    r.BasicProperties.Headers));
            }
            return lidas.Count >= quantidade;
        }, $"esperava {quantidade} mensagem(ns) na fila {fila}; chegaram {lidas.Count}");
        return lidas;
    }

    /// <summary>Mensagens prontas (não entregues) na fila.</summary>
    public async Task<uint> ContarAsync(string fila) => await _canal.MessageCountAsync(fila);

    /// <summary>Publica direto na exchange do teste, sem Outbox (para simular duplicata, lixo, mensagem sem id...).</summary>
    public async Task PublicarCruAsync(string routingKey, string? messageId, string corpo)
    {
        var propriedades = new BasicProperties
        {
            MessageId = messageId,
            Type = routingKey,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
        };
        await _canal.BasicPublishAsync(Topologia.Exchange, routingKey, mandatory: false, propriedades, Encoding.UTF8.GetBytes(corpo));
    }

    /// <summary>Uma porta TCP local onde ninguém escuta: um broker "fora do ar" de verdade (connection refused).</summary>
    public static int PortaSemNinguem()
    {
        var ouvinte = new TcpListener(IPAddress.Loopback, 0);
        ouvinte.Start();
        var porta = ((IPEndPoint)ouvinte.LocalEndpoint).Port;
        ouvinte.Stop();
        return porta;
    }

    public async ValueTask DisposeAsync()
    {
        await _canal.DisposeAsync();
        await _conexao.DisposeAsync();
    }
}
