using F6M05.Sagas.Mensageria;
using F6M05.Sagas.Retry;
using F6M05.Sagas.Saga;
using RabbitMQ.Client;

namespace F6M05.Sagas.Coreografia;

/// <summary>
/// PRONTO. Base de um serviço da COREOGRAFIA: assina alguns eventos num exchange topic
/// compartilhado e, para cada um, talvez publique outro evento no mesmo exchange. Ninguém
/// coordena; o fluxo "emerge" das reações.
/// </summary>
/// <remarks>
/// Contraste com a orquestração: aqui não existe <see cref="SagaPedido"/>. Para responder
/// "em que pé está o pedido X?", alguém precisa juntar os eventos de todos os serviços.
/// </remarks>
public abstract class ServicoCoreografado : IAsyncDisposable
{
    private CanalDePublicacao? _publicacao;
    private ConsumidorComRetry? _consumidor;
    private string _exchange = "";

    /// <summary>Nome curto do serviço (vira parte do nome da fila).</summary>
    public abstract string Nome { get; }

    /// <summary>Tipos de evento que o serviço assina (routing keys).</summary>
    public abstract IReadOnlyList<string> Interesses { get; }

    /// <summary>A reação do serviço a um evento: outro evento para publicar, ou <c>null</c>.</summary>
    public abstract MensagemSaga? Reagir(MensagemSaga evento);

    /// <summary>Declara a fila do serviço (com retry/DLQ), assina os interesses e começa a consumir.</summary>
    public async Task IniciarAsync(IConnection conexao, string exchange, PoliticaDeRetry politica, CancellationToken ct = default)
    {
        _exchange = exchange;
        _publicacao = await CanalDePublicacao.CriarAsync(conexao, ct);
        var canal = _publicacao.Canal;
        var fila = $"{exchange}.{Nome}";

        await canal.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
        await TopologiaDeRetry.DeclararAsync(canal, fila, politica, ct);
        foreach (var interesse in Interesses)
            await canal.QueueBindAsync(fila, exchange, interesse, cancellationToken: ct);

        _consumidor = new ConsumidorComRetry(conexao, fila, politica, async (mensagem, token) =>
        {
            var reacao = Reagir(mensagem.Ler<MensagemSaga>());
            if (reacao is not null)
                await PublicarEventoAsync(_publicacao, exchange, reacao, token);
        });
        await _consumidor.IniciarAsync(ct);
    }

    /// <summary>Publica um evento da coreografia (routing key = tipo, JSON polimórfico).</summary>
    public static Task PublicarEventoAsync(CanalDePublicacao canal, string exchange, MensagemSaga evento, CancellationToken ct = default) =>
        canal.PublicarJsonAsync<MensagemSaga>(exchange, evento.Tipo, evento, evento.MessageId, evento.Tipo, evento.PedidoId.ToString(), ct);

    /// <summary>MessageId determinístico de um evento de reação (reprocessar não gera evento "novo").</summary>
    public static string IdDoEvento<TEvento>(Guid pedidoId) where TEvento : MensagemSaga => $"{pedidoId:N}:{typeof(TEvento).Name}";

    public async ValueTask DisposeAsync()
    {
        if (_consumidor is not null) await _consumidor.DisposeAsync();
        if (_publicacao is not null) await _publicacao.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
