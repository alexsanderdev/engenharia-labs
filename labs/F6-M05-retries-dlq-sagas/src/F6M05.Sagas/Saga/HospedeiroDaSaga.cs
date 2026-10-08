using F6M05.Sagas.Mensageria;
using F6M05.Sagas.Retry;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace F6M05.Sagas.Saga;

/// <summary>
/// PRONTO. Liga a saga ao RabbitMQ: declara o exchange de comandos e a fila de eventos (com a
/// topologia de retry/DLQ) e consome a fila de eventos com o <see cref="ConsumidorComRetry"/>,
/// entregando cada mensagem ao <see cref="OrquestradorSagaPedido"/>.
/// </summary>
/// <remarks>
/// Repare que tudo o que você construiu na parte 1 protege a saga de graça: conflito de
/// concorrência esgotado e "saga não encontrada" são transitórios (retry atrasado); JSON
/// inválido é permanente (DLQ).
/// </remarks>
public sealed class HospedeiroDaSaga : IAsyncDisposable
{
    private readonly CanalDePublicacao _publicacao;
    private readonly ConsumidorComRetry _consumidor;

    private HospedeiroDaSaga(CanalDePublicacao publicacao, ConsumidorComRetry consumidor)
    {
        _publicacao = publicacao;
        _consumidor = consumidor;
    }

    public static async Task<HospedeiroDaSaga> IniciarAsync(
        IConnection conexao,
        NomesDaSaga nomes,
        IRepositorioDeSagas repositorio,
        TimeProvider tempo,
        PoliticaDeRetry politica,
        OpcoesDaSaga? opcoes = null,
        ILoggerFactory? logs = null,
        CancellationToken ct = default)
    {
        var publicacao = await CanalDePublicacao.CriarAsync(conexao, ct);
        await publicacao.Canal.ExchangeDeclareAsync(nomes.ExchangeDeComandos, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: ct);
        await TopologiaDeRetry.DeclararAsync(publicacao.Canal, nomes.FilaDeEventos, politica, ct);

        var orquestrador = new OrquestradorSagaPedido(
            repositorio, new PublicadorDeComandosRabbit(publicacao, nomes), tempo, opcoes,
            logs?.CreateLogger<OrquestradorSagaPedido>());

        var consumidor = new ConsumidorComRetry(
            conexao, nomes.FilaDeEventos, politica,
            (mensagem, token) => orquestrador.ProcessarAsync(mensagem.Ler<MensagemSaga>(), token),
            tempo: tempo, logger: logs?.CreateLogger<ConsumidorComRetry>());
        await consumidor.IniciarAsync(ct);

        return new HospedeiroDaSaga(publicacao, consumidor);
    }

    public async ValueTask DisposeAsync()
    {
        await _consumidor.DisposeAsync();
        await _publicacao.DisposeAsync();
    }
}
