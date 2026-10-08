using System.Text; // Encoding.UTF8.GetString(entrega.Body.Span)
using F6M04.Notificacoes.Contratos;
using F6M04.Notificacoes.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace F6M04.Notificacoes.Mensageria;

/// <summary>O que o worker fez com uma entrega.</summary>
public enum DesfechoDaEntrega
{
    /// <summary>Efeito aplicado; ack.</summary>
    Processada,

    /// <summary>Já processada antes (Inbox); ack sem repetir o efeito.</summary>
    Duplicada,

    /// <summary>Tipo não tratado por este consumidor; ack.</summary>
    Ignorada,

    /// <summary>Nack SEM requeue: a mensagem vai para a DLQ (inválida, ou falhou de novo numa reentrega).</summary>
    Rejeitada,

    /// <summary>Nack COM requeue: falha possivelmente transitória na primeira entrega; o broker reentrega.</summary>
    Devolvida,
}

/// <summary>
/// Worker que consome a fila de Notificações no RabbitMQ (ack manual) e delega cada entrega ao
/// <see cref="ConsumidorDeNotificacoes"/>, num escopo de DI próprio.
/// </summary>
public sealed partial class WorkerDeNotificacoes(
    IServiceScopeFactory escopos,
    IOptions<NotificacoesRabbitMqOptions> opcoes,
    ILogger<WorkerDeNotificacoes> log) : BackgroundService
{
    private readonly NotificacoesRabbitMqOptions _opcoes = opcoes.Value;
    private readonly TaskCompletionSource _pronto = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _processadas, _duplicadas, _ignoradas, _rejeitadas, _devolvidas;

    /// <summary>Completa quando o consumidor está registrado no broker (útil em testes e health checks).</summary>
    public Task Pronto => _pronto.Task;

    // Contadores (em produção, viram métricas do OpenTelemetry).
    public int Processadas => Volatile.Read(ref _processadas);
    public int Duplicadas => Volatile.Read(ref _duplicadas);
    public int Ignoradas => Volatile.Read(ref _ignoradas);
    public int Rejeitadas => Volatile.Read(ref _rejeitadas);
    public int Devolvidas => Volatile.Read(ref _devolvidas);

    /// <summary>PRONTO. Conecta, declara a topologia, liga o consumidor com ack manual e espera o encerramento.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var fabrica = new ConnectionFactory
            {
                Uri = new Uri(_opcoes.ConnectionString),
                ClientProvidedName = "f6m04-worker-notificacoes",
            };
            await using var conexao = await fabrica.CreateConnectionAsync(stoppingToken);
            await using var canal = await conexao.CreateChannelAsync(cancellationToken: stoppingToken);

            await TopologiaDeNotificacoes.DeclararAsync(canal, _opcoes, stoppingToken);
            await canal.BasicQosAsync(prefetchSize: 0, prefetchCount: _opcoes.Prefetch, global: false, stoppingToken);

            var consumidor = new AsyncEventingBasicConsumer(canal);
            consumidor.ReceivedAsync += async (_, entrega) =>
            {
                var desfecho = await TratarEntregaAsync(canal, entrega, stoppingToken);
                Contar(desfecho);
            };

            await canal.BasicConsumeAsync(_opcoes.Fila, autoAck: false, consumidor, stoppingToken);
            _pronto.TrySetResult();

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Encerramento normal: mensagens sem ack voltam para a fila quando o canal fecha.
        }
        catch (Exception ex)
        {
            _pronto.TrySetException(ex);
            throw;
        }
    }

    /// <summary>
    /// Trata UMA entrega e devolve o desfecho. Regras:
    /// <list type="number">
    /// <item>Monte a <see cref="MensagemRecebida"/> com <c>BasicProperties.MessageId</c>, <c>BasicProperties.Type</c> e o corpo UTF-8.</item>
    /// <item>Crie um escopo de DI, resolva o <see cref="ConsumidorDeNotificacoes"/> e chame <c>ProcessarAsync</c>.</item>
    /// <item>Só DEPOIS do commit (o retorno de <c>ProcessarAsync</c>) faça <c>BasicAckAsync</c> — inclusive para duplicadas.</item>
    /// <item><see cref="MensagemInvalidaException"/>: <c>BasicNackAsync(requeue: false)</c> → DLQ.</item>
    /// <item>Outra exceção: na primeira entrega, nack com requeue (<see cref="DesfechoDaEntrega.Devolvida"/>);
    /// se <c>entrega.Redelivered</c>, nack sem requeue (<see cref="DesfechoDaEntrega.Rejeitada"/>).</item>
    /// </list>
    /// </summary>
    public Task<DesfechoDaEntrega> TratarEntregaAsync(IChannel canal, BasicDeliverEventArgs entrega, CancellationToken ct)
    {
        _ = (canal, entrega, escopos);
        throw new NotImplementedException(
            "TODO (Passo 8): monte a MensagemRecebida, processe num escopo novo com o ConsumidorDeNotificacoes, " +
            "faça BasicAckAsync DEPOIS do retorno (inclusive para duplicadas); MensagemInvalidaException → BasicNackAsync(requeue: false); " +
            "outra exceção → requeue só se !entrega.Redelivered. Use LogMensagemInvalida/LogFalhaAoProcessar.");
    }

    private void Contar(DesfechoDaEntrega desfecho)
    {
        switch (desfecho)
        {
            case DesfechoDaEntrega.Processada: Interlocked.Increment(ref _processadas); break;
            case DesfechoDaEntrega.Duplicada: Interlocked.Increment(ref _duplicadas); break;
            case DesfechoDaEntrega.Ignorada: Interlocked.Increment(ref _ignoradas); break;
            case DesfechoDaEntrega.Rejeitada: Interlocked.Increment(ref _rejeitadas); break;
            case DesfechoDaEntrega.Devolvida: Interlocked.Increment(ref _devolvidas); break;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Mensagem {MessageId} ({Tipo}) inválida: enviada para a DLQ")]
    private partial void LogMensagemInvalida(string messageId, string tipo, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao processar {MessageId} ({Tipo}); devolvida para a fila: {Devolvida}")]
    private partial void LogFalhaAoProcessar(string messageId, string tipo, bool devolvida, Exception ex);
}
